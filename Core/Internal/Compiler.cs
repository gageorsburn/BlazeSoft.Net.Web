using System;
using System.Collections.Generic;
using BlazeSoft.Net.Web.Contract;

namespace BlazeSoft.Net.Web.Core
{ 
    /*public interface Compiler
    {
        Compiler Instance { get; }
        CompilerResult Compile(PageReversion reversion, IEnumerable<string> references);
    }

    public class CSharpCompiler : Compiler
    {
        Compiler Compiler.Instance
        {
            get
            {
                return new CSharpCompiler();
            }
        }

        CompilerResult Compiler.Compile(PageReversion reversion, IEnumerable<string> references)
        {
            throw new NotImplementedException();
        }
    }

    public class VisualBasicCompiler : Compiler
    {
        Compiler Compiler.Instance
        {
            get
            {
                return new VisualBasicCompiler();
            }
        }

        CompilerResult Compiler.Compile(PageReversion reversion, IEnumerable<string> references)
        {
            throw new NotImplementedException();
        }
    }

    public class CompilerResult//()
    {
        public string Output { get; set; }
        public byte[] AssemblyData { get; set; }

        public CompilerDiagnosticMessage[] DiagnosticMessages { get; set; }
    }

    public class CompilerDiagnosticMessage//(string classFile, int startLine, int startCharacter, int endLine, int endCharacter, bool isWarning, string message)
    {
        public string ClassFile { get; set; } //= classFile;

        public int StartLine { get; set; } //= startLine;
        public int StartCharacter { get; set; }// = startCharacter;
        public int EndLine { get; set; }// = endLine;
        public int EndCharacter { get; set; }// = endCharacter;

        public bool IsWarning { get; set; }// = isWarning;

        public string Message { get; set; }// = message;
    }*/
}