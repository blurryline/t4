// 
// GenerationTests.cs
//  
// Author:
//       Mikayla Hutchinson <m.j.hutchinson@gmail.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Microsoft.VisualStudio.TextTemplating;
using System.Linq;
using System.CodeDom.Compiler;

namespace Mono.TextTemplating.Tests
{
	
	
	[TestFixture]
	public class GenerationTests
	{	
		[Test]
		public void TemplateGeneratorTest ()
		{
			var gen = new TemplateGenerator ();
			string tmp = null;
			gen.ProcessTemplate (null, "<#@ template language=\"C#\" #>", ref tmp, out tmp);
			Assert.IsNull (gen.Errors.OfType<CompilerError> ().FirstOrDefault (), "ProcessTemplate");
		}

		[Test]
		public void ImportReferencesTest ()
		{
			var gen = new TemplateGenerator ();
			string tmp = null;
			gen.ReferencePaths.Add (Path.GetDirectoryName (typeof (Uri).Assembly.Location));
			gen.ReferencePaths.Add (Path.GetDirectoryName (typeof (System.Linq.Enumerable).Assembly.Location));
			gen.ProcessTemplate (null, "<#@ assembly name=\"System.dll\" #>\n<#@ assembly name=\"System.Core.dll\" #>", ref tmp, out tmp);
			Assert.IsNull (gen.Errors.OfType<CompilerError> ().FirstOrDefault (), "ProcessTemplate");
		}

		[Test]
		public void IncludeFileThatDoesNotExistTest ()
		{
			var gen = new TemplateGenerator ();
			string tmp = null;
			gen.ProcessTemplate (null, "<#@ include file=\"none.tt\" #>", ref tmp, out tmp);
			Assert.IsTrue (gen.Errors.OfType<CompilerError> ().First ().ErrorText
				.StartsWith ("Could not read included file 'none.tt'"));
		}

		[Test]
		public void Generate ()
		{
			string Input = ParsingTests.ParseSample1;
			string Output = OutputSample1;
			Generate (Input, Output, "\n");
		}
		
		[Test]
		public void GenerateMacNewlines ()
		{
			string MacInput = ParsingTests.ParseSample1.Replace ("\n", "\r");
			string MacOutput = OutputSample1.Replace ("\\n", "\\r").Replace ("\n", "\r");;
			Generate (MacInput, MacOutput, "\r");
		}
		
		[Test]
		public void GenerateWindowsNewlines ()
		{
			string WinInput = ParsingTests.ParseSample1.Replace ("\n", "\r\n");
			string WinOutput = OutputSample1.Replace ("\\n", "\\r\\n").Replace ("\n", "\r\n");
			Generate (WinInput, WinOutput, "\r\n");
		}

		[Test]
		public void DefaultLanguage ()
		{
			DummyHost host = new DummyHost ();
			string template = @"<#= DateTime.Now #>";
			ParsedTemplate pt = ParsedTemplate.FromText (template, host);
			Assert.AreEqual (0, host.Errors.Count);
			TemplateSettings settings = TemplatingEngine.GetSettings (host, pt);
			Assert.AreEqual (settings.Language, "C#");
		}
		
		//NOTE: we set the newline property on the code generator so that the whole files has matching newlines,
		// in order to match the newlines in the verbatim code blocks
		void Generate (string input, string expectedOutput, string newline)
		{
			DummyHost host = new DummyHost ();
			string className = "GeneratedTextTransformation4f504ca0";
			string code = GenerateCode (host, input, className, newline);
			Assert.AreEqual (0, host.Errors.Count);

			var generated = TemplatingEngineHelper.CleanCodeDom (code, newline);
			expectedOutput = TemplatingEngineHelper.CleanCodeDom (expectedOutput, newline);
			Assert.AreEqual (expectedOutput, generated);
		}
		
		#region Helpers
		
		string GenerateCode (ITextTemplatingEngineHost host, string content, string name, string generatorNewline)
		{
			ParsedTemplate pt = ParsedTemplate.FromText (content, host);
			if (pt.Errors.HasErrors) {
				host.LogErrors (pt.Errors);
				return null;
			}
			
			TemplateSettings settings = TemplatingEngine.GetSettings (host, pt);
			if (name != null)
				settings.Name = name;
			if (pt.Errors.HasErrors) {
				host.LogErrors (pt.Errors);
				return null;
			}
			
			var ccu = TemplatingEngine.GenerateCompileUnit (host, content, pt, settings);
			if (pt.Errors.HasErrors) {
				host.LogErrors (pt.Errors);
				return null;
			}
			
			var opts = new System.CodeDom.Compiler.CodeGeneratorOptions ();
			using (var writer = new System.IO.StringWriter ()) {
				writer.NewLine = generatorNewline;
				settings.Provider.GenerateCodeFromCompileUnit (ccu, writer, opts);
				return writer.ToString ();
			}
		}

		#endregion

		#region Expected output strings

		public static string OutputSample1 =
@"
namespace Microsoft.VisualStudio.TextTemplating {
    
    
    public partial class GeneratedTextTransformation4f504ca0 : global::Microsoft.VisualStudio.TextTemplating.TextTransformation {
        
        
        #line 9 """"

var s = ""baz \\#>"";

        #line default
        #line hidden
        
        public override string TransformText() {
            this.GenerationEnvironment = null;
            
            #line 2 """"
            this.Write(""Line One\nLine Two\n"");
            
            #line default
            #line hidden
            
            #line 4 """"

var foo = 5;

            
            #line default
            #line hidden
            
            #line 7 """"
            this.Write(""Line Three "");
            
            #line default
            #line hidden
            
            #line 7 """"
            this.Write(global::Microsoft.VisualStudio.TextTemplating.ToStringHelper.ToStringWithCulture( bar ));
            
            #line default
            #line hidden
            
            #line 7 """"
            this.Write(""\nLine Four\n"");
            
            #line default
            #line hidden
            return this.GenerationEnvironment.ToString();
        }
        
        public override void Initialize() {
            base.Initialize();
        }
    }
}
";
		#endregion
	}
}
