namespace RequireNamedArgs.Tests.Support


    [<RequireQualifiedAccess>]
    module Format =


        let klass (source: string) =
            sprintf
                "class Wombat
                {
                    %s
                }" source


        let program (source: string) =
            sprintf
                "using System;
                namespace Frobnitz
                {
                    %s
                    
                    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method)]
                    class RequireNamedArgsAttribute : Attribute {}
                    
                    class Program { static void Main(string[] args) {} }
                }" source
