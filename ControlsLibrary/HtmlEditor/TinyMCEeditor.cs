using CefSharp;
using CefSharp.WinForms;
using CefSharp.WinForms.Experimental;
using ControlsLibrary.HtmlEditor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FB.UserControls
{
    public partial class TinyMCEeditor : UserControl
    {
        public const string RenderProcessCrashedUrl = "http://processcrashed";
        #region properties
        public bool IsDirty { get { return getIsDirty(); } set { setIsDirty(value); } }
        public IWinFormsWebBrowser Browser { get; private set; }
        #endregion
        private IntPtr browserHandle;
        private ChromiumWidgetNativeWindow messageInterceptor;
        private bool multiThreadedMessageLoopEnabled;

        public TinyMCEeditor(string url, bool multiThreadedMessageLoopEnabled)
        {
            InitializeComponent();

            var browser = new ChromiumWebBrowser(url)
            {
                Dock = DockStyle.Fill
            };

            browserPanel.Controls.Add(browser);

            Browser = browser;

            browser.MenuHandler = new MenuHandler();
            browser.RequestHandler = new WinFormsRequestHandler(null);// openNewTab);
            //browser.JsDialogHandler = new JsDialogHandler();
            //browser.DownloadHandler = new DownloadHandler();
            //browser.AudioHandler = new AudioHandler();

            if (multiThreadedMessageLoopEnabled)
            {
                browser.KeyboardHandler = new KeyboardHandler();
            }
            else
            {
                //When MultiThreadedMessageLoop is disabled we don't need the
                //CefSharp focus handler implementation.
                browser.FocusHandler = null;
            }

            //The CefSharp.WinForms.Handler.LifeSpanHandler implementation
            //allows for Popups to be hosted in Controls/Tabs
            //This example also demonstrates docking DevTools in a SplitPanel
            browser.LifeSpanHandler = LifeSpanHandler
                .Create()
                .OnPopupCreated((ctrl, targetUrl) =>
                {
                    ////Don't try using ctrl.FindForm() here as
                    ////the control hasn't been attached to a parent yet.
                    //if (FindForm() is BrowserForm owner)
                    //{
                    //    owner.AddTab(ctrl, targetUrl);
                    //}
                })
                .OnPopupDestroyed((ctrl, popupBrowser) =>
                {
                    //If we docked  DevTools (hosted it ourselves rather than the default popup)
                    //Used when the BrowserTabUserControl.ShowDevToolsDocked method is called
                    if (popupBrowser.MainFrame.Url.Equals("devtools://devtools/devtools_app.html"))
                    {
                        //Dispose of the parent control we used to host DevTools, this will release the DevTools window handle
                        //and the ILifeSpanHandler.OnBeforeClose() will be call after.
                        ctrl.Dispose();
                    }
                    else
                    {
                        //If browser is disposed or the handle has been released then we don't
                        //need to remove the tab in this example. The user likely used the
                        // File -> Close Tab menu option which also calls BrowserForm.RemoveTab
                        if (!ctrl.IsDisposed && ctrl.IsHandleCreated)
                        {
                            //if (ctrl.FindForm() is BrowserForm owner)
                            //{
                            //    var windowHandle = popupBrowser.GetHost().GetWindowHandle();

                            //    owner.RemoveTab(windowHandle);
                            //}

                            ctrl.Dispose();
                        }
                    }
                })
                .Build();

            browser.LoadingStateChanged += OnBrowserLoadingStateChanged;
            browser.ConsoleMessage += OnBrowserConsoleMessage;
            browser.TitleChanged += OnBrowserTitleChanged;
            browser.AddressChanged += OnBrowserAddressChanged;
            browser.StatusMessage += OnBrowserStatusMessage;
            browser.IsBrowserInitializedChanged += OnIsBrowserInitializedChanged;
            browser.LoadError += OnLoadError;

#if NETCOREAPP
            browser.JavascriptObjectRepository.Register("boundAsync", new AsyncBoundObject(), options: BindingOptions.DefaultBinder);
#else
            //browser.JavascriptObjectRepository.Register("bound", new BoundObject(), isAsync: false, options: BindingOptions.DefaultBinder);
            //browser.JavascriptObjectRepository.Register("boundAsync", new AsyncBoundObject(), isAsync: true, options: BindingOptions.DefaultBinder);
#endif

            //If you call CefSharp.BindObjectAsync in javascript and pass in the name of an object which is not yet
            //bound, then ResolveObject will be called, you can then register it
            browser.JavascriptObjectRepository.ResolveObject += (sender, e) =>
            {
                var repo = e.ObjectRepository;
                if (e.ObjectName == "boundAsync2")
                {
#if NETCOREAPP
                    repo.Register("boundAsync2", new AsyncBoundObject(), options: BindingOptions.DefaultBinder);
#else
                    //repo.Register("boundAsync2", new AsyncBoundObject(), isAsync: true, options: BindingOptions.DefaultBinder);
#endif
                }
            };

            browser.RenderProcessMessageHandler = new RenderProcessMessageHandler();
            browser.DisplayHandler = new DisplayHandler();
            //browser.MouseDown += OnBrowserMouseClick;
            browser.HandleCreated += OnBrowserHandleCreated;
            //browser.ResourceHandlerFactory = new FlashResourceHandlerFactory();
            this.multiThreadedMessageLoopEnabled = multiThreadedMessageLoopEnabled;

            //var eventObject = new ScriptedMethodsBoundObject();
            //eventObject.EventArrived += OnJavascriptEventArrived;
            // Use the default of camelCaseJavascriptNames
            // .Net methods starting with a capitol will be translated to starting with a lower case letter when called from js
#if !NETCOREAPP
            //browser.JavascriptObjectRepository.Register("boundEvent", eventObject, isAsync: false, options: BindingOptions.DefaultBinder);
#endif

            //CefExample.RegisterTestResources(browser);

            var version = string.Format("Chromium: {0}, CEF: {1}, CefSharp: {2}", Cef.ChromiumVersion, Cef.CefVersion, Cef.CefSharpVersion);
            //Set label directly, don't use DisplayOutput as call would be a NOOP (no valid handle yet).
            //outputLabel.Text = version;
        }
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                    components = null;
                }

                if (messageInterceptor != null)
                {
                    messageInterceptor.ReleaseHandle();
                    messageInterceptor = null;
                }
            }
            base.Dispose(disposing);
        }

        private void OnBrowserHandleCreated(object sender, EventArgs e)
        {
            browserHandle = ((ChromiumWebBrowser)Browser).Handle;
        }

        private void OnBrowserMouseClick(object sender, MouseEventArgs e)
        {
            MessageBox.Show("Mouse Clicked" + e.X + ";" + e.Y + ";" + e.Button);
        }

        private void OnLoadError(object sender, LoadErrorEventArgs args)
        {
            //Don't display an error for external protocols that we allow the OS to
            //handle in OnProtocolExecution().
            if (args.ErrorCode == CefErrorCode.UnknownUrlScheme && args.Frame.Url.StartsWith("mailto"))
            {
                return;
            }

            DisplayOutput("Load Error:" + args.ErrorCode + ";" + args.ErrorText);
        }

        private void OnBrowserConsoleMessage(object sender, ConsoleMessageEventArgs args)
        {
            DisplayOutput(string.Format("Line: {0}, Source: {1}, Message: {2}", args.Line, args.Source, args.Message));
        }

        private void OnBrowserStatusMessage(object sender, StatusMessageEventArgs args)
        {
            //this.InvokeOnUiThreadIfRequired(() => statusLabel.Text = args.Value);
        }

        private void OnBrowserLoadingStateChanged(object sender, LoadingStateChangedEventArgs args)
        {
            SetCanGoBack(args.CanGoBack);
            SetCanGoForward(args.CanGoForward);

            this.InvokeOnUiThreadIfRequired(() => SetIsLoading(args.IsLoading));
        }

        private void OnBrowserTitleChanged(object sender, TitleChangedEventArgs args)
        {
            this.InvokeOnUiThreadIfRequired(() => Parent.Text = args.Title);
        }

        private void OnBrowserAddressChanged(object sender, AddressChangedEventArgs args)
        {
            //this.InvokeOnUiThreadIfRequired(() => urlTextBox.Text = args.Address);
        }

        private static void OnJavascriptEventArrived(string eventName, object eventData)
        {
            switch (eventName)
            {
                case "click":
                    {
                        var message = eventData.ToString();
                        var dataDictionary = eventData as Dictionary<string, object>;
                        if (dataDictionary != null)
                        {
                            var result = string.Join(", ", dataDictionary.Select(pair => pair.Key + "=" + pair.Value));
                            message = "event data: " + result;
                        }
                        MessageBox.Show(message, "Javascript event arrived", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    }
            }
        }

        private void SetCanGoBack(bool canGoBack)
        {
            //this.InvokeOnUiThreadIfRequired(() => backButton.Enabled = canGoBack);
        }

        private void SetCanGoForward(bool canGoForward)
        {
            //this.InvokeOnUiThreadIfRequired(() => forwardButton.Enabled = canGoForward);
        }

        private void SetIsLoading(bool isLoading)
        {
            //goButton.Text = isLoading ?
            //    "Stop" :
            //    "Go";
            //goButton.Image = isLoading ?
            //    Properties.Resources.nav_plain_red :
            //    Properties.Resources.nav_plain_green;

            HandleToolStripLayout();
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private void OnIsBrowserInitializedChanged(object sender, EventArgs e)
        {
            //Get the underlying browser host wrapper
            var browserHost = Browser.GetBrowser().GetHost();
            var requestContext = browserHost.RequestContext;
            string errorMessage;
            // Browser must be initialized before getting/setting preferences
            var success = requestContext.SetPreference("enable_do_not_track", true, out errorMessage);
            if (!success)
            {
                this.InvokeOnUiThreadIfRequired(() => MessageBox.Show("Unable to set preference enable_do_not_track errorMessage: " + errorMessage));
            }

            //Example of disable spellchecking
            //success = requestContext.SetPreference("browser.enable_spellchecking", false, out errorMessage);

            var preferences = requestContext.GetAllPreferences(true);
            var doNotTrack = (bool)preferences["enable_do_not_track"];

            //Use this to check that settings preferences are working in your code
            //success = requestContext.SetPreference("webkit.webprefs.minimum_font_size", 24, out errorMessage);

            //If we're using CefSetting.MultiThreadedMessageLoop (the default) then to hook the message pump,
            // which running in a different thread we have to use a NativeWindow
            if (multiThreadedMessageLoopEnabled)
            {
                SetupMessageInterceptor();
            }
        }

        /// <summary>
        /// The ChromiumWebBrowserControl does not fire MouseEnter/Move/Leave events, because Chromium handles these.
        /// This method provides a demo of hooking the Chrome_RenderWidgetHostHWND handle to receive low level messages.
        /// You can likely hook other window messages using this technique, drag/drog etc
        /// </summary>
        private void SetupMessageInterceptor()
        {
            if (messageInterceptor != null)
            {
                messageInterceptor.ReleaseHandle();
                messageInterceptor = null;
            }

            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        IntPtr chromeWidgetHostHandle;
                        if (ChromiumRenderWidgetHandleFinder.TryFindHandle(Browser, out chromeWidgetHostHandle))
                        {
                            messageInterceptor = new ChromiumWidgetNativeWindow((Control)Browser, chromeWidgetHostHandle);

                            messageInterceptor.OnWndProc(message =>
                            {
                                const int WM_MOUSEACTIVATE = 0x0021;
                                const int WM_NCLBUTTONDOWN = 0x00A1;
                                const int WM_DESTROY = 0x0002;

                                // Render process switch happened, need to find the new handle
                                if (message.Msg == WM_DESTROY)
                                {
                                    SetupMessageInterceptor();
                                    return false;
                                }

                                if (message.Msg == WM_MOUSEACTIVATE)
                                {
                                    // The default processing of WM_MOUSEACTIVATE results in MA_NOACTIVATE,
                                    // and the subsequent mouse click is eaten by Chrome.
                                    // This means any .NET ToolStrip or ContextMenuStrip does not get closed.
                                    // By posting a WM_NCLBUTTONDOWN message to a harmless co-ordinate of the
                                    // top-level window, we rely on the ToolStripManager's message handling
                                    // to close any open dropdowns:
                                    // http://referencesource.microsoft.com/#System.Windows.Forms/winforms/Managed/System/WinForms/ToolStripManager.cs,1249
                                    var topLevelWindowHandle = message.WParam;
                                    PostMessage(topLevelWindowHandle, WM_NCLBUTTONDOWN, IntPtr.Zero, IntPtr.Zero);
                                }
                                //Forward mouse button down message to browser control
                                //else if(message.Msg == WM_LBUTTONDOWN)
                                //{
                                //    PostMessage(browserHandle, WM_LBUTTONDOWN, message.WParam, message.LParam);
                                //}

                                // The ChromiumWebBrowserControl does not fire MouseEnter/Move/Leave events, because Chromium handles these.
                                // However we can hook into Chromium's messaging window to receive the events.
                                //
                                //const int WM_MOUSEMOVE = 0x0200;
                                //const int WM_MOUSELEAVE = 0x02A3;
                                //
                                //switch (message.Msg) {
                                //    case WM_MOUSEMOVE:
                                //        Console.WriteLine("WM_MOUSEMOVE");
                                //        break;
                                //    case WM_MOUSELEAVE:
                                //        Console.WriteLine("WM_MOUSELEAVE");
                                //        break;
                                //}

                                return false;
                            });

                            break;
                        }
                        else
                        {
                            // Chrome hasn't yet set up its message-loop window.
                            await Task.Delay(10);
                        }
                    }
                }
                catch
                {
                    // Errors are likely to occur if browser is disposed, and no good way to check from another thread
                }
            });
        }

        private void DisplayOutput(string output)
        {
            //outputLabel.InvokeOnUiThreadIfRequired(() => outputLabel.Text = output);
        }

        private void HandleToolStripLayout(object sender, LayoutEventArgs e)
        {
            HandleToolStripLayout();
        }

        private void HandleToolStripLayout()
        {
            //var width = toolStrip1.Width;
            //foreach (ToolStripItem item in toolStrip1.Items)
            //{
            //    if (item != urlTextBox)
            //    {
            //        width -= item.Width - item.Margin.Horizontal;
            //    }
            //}
            //urlTextBox.Width = Math.Max(0, width - urlTextBox.Margin.Horizontal - 18);
        }













        #region mymethod


        private bool setIsDirty(bool value)
        {
            string script = string.Format(@"tinymce.activeEditor.setDirty({0});", value);

            var javascriptResponse = Browser.EvaluateScriptAsync(script).Result;
            return javascriptResponse.Result == null ? false : (bool)javascriptResponse.Result;
        }

        private bool getIsDirty()
        {
            const string script = @"tinymce.activeEditor.isDirty();";

            var javascriptResponse = Browser.EvaluateScriptAsync(script).Result;
            return javascriptResponse.Result == null ? false : (bool)javascriptResponse.Result;
        }
        #endregion
    }
}
