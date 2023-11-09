namespace RequireNamedArgs.Tests.Support


    [<RequireQualifiedAccess>]
    module Format =


        let klass (source: string) =
            sprintf
                "class Character
                {
                    %s
                }" source


        let program (source: string) =
            sprintf
                "using System;
                namespace DragonBall
                {
                    %s
                    
                    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct)]
                    class RequireNamedArgsAttribute : Attribute {}
                    
                    class Program { static void Main(string[] args) {} }
                }" source
