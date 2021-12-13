using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Callbacks;
using System.Linq;
 
public class HookEditor 
{
    static List<string> assemblyPathss = new List<string>()
    {
        Application.dataPath+"/../Library/ScriptAssemblies/Assembly-CSharp.dll",
        Application.dataPath+"/../Library/ScriptAssemblies/Assembly-CSharp-firstpass.dll",         
 
    };
 
    [MenuItem("Hook/主动注入代码")]
    static void ReCompile()
    {
        AssemblyPostProcessorRun();
    }
 
    [MenuItem("Hook/输出结果")]
    static void HookUtilsMessage()
    {
        HookUtils.ToMessage();
    }
 
    //[PostProcessScene]//打包的时候会自动调用下面方法注入代码
    static void AssemblyPostProcessorRun()
    {
        try
        {
            Debug.Log("AssemblyPostProcessor running");
            EditorApplication.LockReloadAssemblies();
            DefaultAssemblyResolver assemblyResolver = new DefaultAssemblyResolver();
 
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if(!assembly.IsDynamic)
                {
                    assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(assembly.Location));
                }else
                {
                    Debug.Log(assembly.IsDynamic);
                    Debug.Log(assembly.FullName);
                }    
            }
 
            assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(EditorApplication.applicationPath) + "/Data/Managed");
 
            ReaderParameters readerParameters = new ReaderParameters();
            readerParameters.AssemblyResolver = assemblyResolver;
 
            WriterParameters writerParameters = new WriterParameters();
 
 
            foreach (String assemblyPath in assemblyPathss)
            {
                readerParameters.ReadSymbols = true;
                //readerParameters.SymbolReaderProvider = new Mono.Cecil.Mdb.MdbReaderProvider();
                writerParameters.WriteSymbols = true;
                //writerParameters.SymbolWriterProvider = new Mono.Cecil.Mdb.MdbWriterProvider();

                AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters);
                if (HookEditor.ProcessAssembly(assemblyDefinition))
                {
                    Debug.Log("Writing to " + assemblyPath);
                    assemblyDefinition.Write(assemblyPath, writerParameters);
                    Debug.Log("Done writing");
                }
                else
                {
                    Debug.Log(Path.GetFileName(assemblyPath) + " didn't need to be processed");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
        }
        EditorApplication.UnlockReloadAssemblies();
    }
 
    private static bool ProcessAssembly(AssemblyDefinition assemblyDefinition)
    {
        bool wasProcessed = false;
 
        foreach (ModuleDefinition moduleDefinition in assemblyDefinition.Modules)
        {
            foreach (TypeDefinition typeDefinition in moduleDefinition.Types)
            {
                if (typeDefinition.Name == typeof(HookUtils).Name) continue;
                //过滤抽象类
                if (typeDefinition.IsAbstract) continue;
                //过滤抽象方法
                if (typeDefinition.IsInterface) continue;
                foreach (MethodDefinition methodDefinition in typeDefinition.Methods)
                {
                    //过滤构造函数
                    if(methodDefinition.Name == ".ctor")continue;
                    if (methodDefinition.Name == ".cctor") continue;
                    //过滤抽象方法、虚函数、get set 方法
                    if (methodDefinition.IsAbstract) continue;
                    if (methodDefinition.IsVirtual) continue;
                    if (methodDefinition.IsGetter) continue;
                    if (methodDefinition.IsSetter) continue;
                    //如果注入代码失败，可以打开下面的输出看看卡在了那个方法上。
                    //Debug.Log(methodDefinition.Name  +" ===== "+ methodDefinition.Body + "======= " + typeDefinition.Name + "======= " +typeDefinition.BaseType.GenericParameters +" ===== "+ moduleDefinition.Name);
                    MethodReference logMethodReference = moduleDefinition.Import(typeof(HookUtils).GetMethod("Begin", new Type[] { typeof(string) }));
                    MethodReference logMethodReference1 = moduleDefinition.Import(typeof(HookUtils).GetMethod("End", new Type[] { typeof(string) }));
                    //如果注入方法失败可以试试先跳过
                    //if(methodDefinition.Body==null)
                    //{
                    //    Debug.Log(methodDefinition.Name);
                    //    continue;
                    //}
                    ILProcessor ilProcessor = methodDefinition.Body.GetILProcessor();
 
                    Instruction first = methodDefinition.Body.Instructions[0];
                    ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Ldstr, typeDefinition.FullName + "." + methodDefinition.Name));
                    ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Call, logMethodReference));
 
                    //解决方法中直接 return 后无法统计的bug 
                    //https://lostechies.com/gabrielschenker/2009/11/26/writing-a-profiler-for-silverlight-applications-part-1/
 
                    Instruction last = methodDefinition.Body.Instructions[methodDefinition.Body.Instructions.Count - 1];
                    Instruction lastInstruction = Instruction.Create(OpCodes.Ldstr, typeDefinition.FullName + "." + methodDefinition.Name);
                    ilProcessor.InsertBefore(last, lastInstruction);
                    ilProcessor.InsertBefore(last, Instruction.Create(OpCodes.Call, logMethodReference1));
 
                    var jumpInstructions = methodDefinition.Body.Instructions.Cast<Instruction>().Where(i => i.Operand == last);
                    foreach (var jump in jumpInstructions)
                    {
                        jump.Operand = lastInstruction;
                    }
 
                    wasProcessed = true;
                }
            }
        }
 
        return wasProcessed;
    }
}