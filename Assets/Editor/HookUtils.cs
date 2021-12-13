using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using System;
using System.Text;
using System.IO;
using System.Threading;
using System.Collections.Generic;

public class HookUtils
{
    struct Datas
    {
        public string funName;       //函数名字
        public long funMem;          //本次调用函数占用内存
        public float funTime;        //本次调用函数执行时间
        public int funCalls;         //函数被调用次数
        public long funTotalMem;     //函数累计占用内存
        public float funTotalTime;   //函数累计执行时间
        public long sMem;            //记录开始时的内存
        public float sTime;          //记录开始时的时间
    }
    
    static Dictionary<string, Datas> dataRecord = new Dictionary<string, Datas>();
    static Thread mainThread= Thread.CurrentThread;
    
    public static void Begin(string name)
    {
        if(Thread.CurrentThread == mainThread)
        {
            long tmpMen = Profiler.GetTotalAllocatedMemoryLong();
            float tmpTime = Time.realtimeSinceStartup;
            if(dataRecord.ContainsKey(name))
            {
                Datas tmp = dataRecord[name];
                tmp.sMem = tmpMen;
                tmp.sTime = tmpTime;
                dataRecord[name] = tmp;
            }else
            {
                Datas tmp = new Datas();
                tmp.funName = name;
                tmp.funMem = 0L;
                tmp.funTime = 0f;
                tmp.funCalls = 0;
                tmp.funTotalMem = 0L;
                tmp.funTotalTime = 0f;
                tmp.sMem = tmpMen;
                tmp.sTime = tmpTime;
                dataRecord.Add(name, tmp);
            }
        }
    }
    
    public static void End(string name)
    {
        if(Thread.CurrentThread == mainThread)
        {
            long tmpMem = Profiler.GetTotalAllocatedMemoryLong();
            float tmpTime = Time.realtimeSinceStartup;
            Datas tmp = dataRecord[name];
            //过滤因为GC而统计不正确的数据
            if (tmpMem - tmp.sMem >= 0)
            {
                tmp.funMem = tmpMem - tmp.sMem;
                tmp.funTime = tmpTime - tmp.sTime;
                tmp.funTotalMem += tmp.funMem;
                tmp.funTotalTime += tmp.funTime;
                tmp.funCalls += 1;
                tmp.sMem = 0L;
                tmp.sTime = 0f;
                dataRecord[name] = tmp;
            }
        }
    }
    
    public static void ToMessage()
    {
        string nowTime = System.DateTime.Now.ToString("[yyyy-MM-dd]-[HH-mm-ss]"); 
        string fileName = nowTime + ".csv";
        string header = "funName,funMem/k,funAverageMem/k,funTime/s,funAverageTime/s,funCalls"; 
        using (StreamWriter sw = new StreamWriter(fileName))
        {
            sw.WriteLine(header);
            var ge = dataRecord.GetEnumerator();
            while(ge.MoveNext())
            {
                Datas tmp = ge.Current.Value;
                //过滤调用次数0的函数
                if(tmp.funCalls <= 0) continue;
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0},", tmp.funName);
                sb.AppendFormat("{0:f4},", tmp.funMem/1024.0);
                sb.AppendFormat("{0:f4},", tmp.funTotalMem/(tmp.funCalls*1024.0));
                sb.AppendFormat("{0},", tmp.funTime);
                sb.AppendFormat("{0},", tmp.funTotalTime/tmp.funCalls);
                sb.AppendFormat("{0}", tmp.funCalls);
                sw.WriteLine(sb);
            }
        }
        Debug.Log("文件输出完成");
    }
}