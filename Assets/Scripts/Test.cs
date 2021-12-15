using System;
using UnityEngine;
using System.Text;

public class Test1
{
    public static void each()
    {
        StringBuilder sb = new StringBuilder();
        for (int a = 1; a < 20; a = a + 1)
        {
            sb.Append(a);
        }
        Debug.Log("Test1 done");
    } 
}

public class Test2
{
    public static int each(int i)
    {
        if(i%2 == 0)
        {
            Debug.Log("Test2 return 2");
            return 2;
        }
        Debug.Log("Test2 done");
        return i;
    } 
}

public class Test : MonoBehaviour
{
    void Start()
    {
       Test1.each();
       Test2.each(1);
       Test2.each(2);
       Test2.each(3);
    }
}