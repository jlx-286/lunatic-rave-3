using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class EvalMethod : MonoBehaviour{
    private void Start()
    {
        EvalMethod.eval_r("Debug.Log(666);");
    }
    public static readonly string code = "eval_r(str)";
    public static Type evalType = null;
    public static object eval_obj = null;
    static EvalMethod(){
        CodeDomProvider codeDomProvider = CodeDomProvider.CreateProvider("CSharp");
        //ICodeCompiler codeCompiler = codeDomProvider.CreateCompiler();
        CompilerParameters compilerParameters = new CompilerParameters();
        compilerParameters.GenerateInMemory = true;
        CompilerResults compilerResults = codeDomProvider.CompileAssemblyFromSource(compilerParameters, code);
        Assembly assembly = compilerResults.CompiledAssembly;
        eval_obj = Activator.CreateInstance(assembly.GetType("EvalMethod"));
    }
    
    //private void S(){}
    public static object eval_r(string s){
        return evalType.InvokeMember("Eval", BindingFlags.InvokeMethod, null, eval_obj, new object[] { s });
    }
}
