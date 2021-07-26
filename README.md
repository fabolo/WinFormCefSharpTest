# WinFormCefSharpTest
test project for stackOverflow question https://stackoverflow.com/questions/68520171/cefsharp-winform-mdi

I need a winforms control running TinyMCE js editor. This is a project with cefSharp 91.1.23, based on the minimal sample, SimpleBrowserForm. There's a control hosting a ChromiumWebBrowser pointing to an HTML page which then run the Tiny editor, I added it to a form and it display my editor fully functional.

Problem is the existing application that will use the control is an MDI winforms on .NET 4.5.2. When started from an MDI parent my form shows the editor with no focus, typing does not put text in the TinyMCE textarea, I've tried to switch to the more complex sample with multiThreadedMessageLoop enabled with no luck.

I also had problem with the missing CefSharp.Core.Runtime.dll, I solved by copying it manually in the bin\x86\Debug directory
