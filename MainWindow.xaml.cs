using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using WindowsInput;
using System.Timers;
using System.Windows.Media.Animation;
using System.IO;

namespace KiepMissileLauncher
{
    public partial class MainWindow : Window
    {
        private const string LOGFILE = "KiepMissileLauncher.log";
        private Process launcher;
        private ScanningFunctions currentFunction = ScanningFunctions.NoScanning;
        private bool keySayDown = false;
        InputSimulator inputSimulator = new InputSimulator();
        private delegate void DummyDelegate();

        private const int KEY_SCANNING = 107;
        private const int KEY_SAY = 111;
        private const int KEY_YES = 109;
        private const int KEY_NO = 106;

        private enum ScanningFunctions
        {
            NoScanning,
            Up,
            Down,
            Left,
            Right,
            Launch,
            Quit
        }

        public MainWindow()
        {
            InitializeComponent();

            // Wait 1 second before subscribing to keypresses
            WaitSubscribeKeypresses();

            // Log startup
            Log("Start");

            // Start the rocket launcher application
            StartLauncher();

#if !DEBUG
            this.Topmost = true;
            this.Focus();
#endif

            this.Closing += new System.ComponentModel.CancelEventHandler(StopLauncher);
        }


        /// <summary>
        /// Start the rocket launcher application
        /// </summary>
        private void StartLauncher()
        {
            try
            {
                launcher = new Process();

                string baseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                launcher.StartInfo.FileName = baseDir + "\\OriginalMissileLauncher\\Missile_Launcher.exe";

                // Hiding the window, not working?
                launcher.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                launcher.StartInfo.CreateNoWindow = true;
                launcher.StartInfo.UseShellExecute = true;

                launcher.Start();
            }
            catch (Exception ex)
            {
                Log("Error starting Missile_Launcher.exe\t" + ex.Message);

                // Terminate the application if the original launcher application could not be started
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// Stop the rocket launcher application. First try close, then kill.
        /// </summary>
        private void StopLauncher(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (launcher != null)
            {
                try
                {
                    launcher.CloseMainWindow();
                }
                catch (Exception ex)
                {
                    Log("Error closing Missile_Launcher.exe\t" + ex.Message);

                    try
                    {
                        launcher.Kill();
                    }
                    catch (Exception ex2)
                    {
                        Log("Error killing Missile_Launcher.exe\t" + ex2.Message);
                    }
                }
            }
        }

        private void WaitSubscribeKeypresses()
        {
            try
            {
                Timer timer = new Timer(1000);
                timer.Elapsed += delegate
                {
                    this.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    (DummyDelegate)
                    delegate
                    {
                        timer.Enabled = false;

                        // Block keys from being received by other applications
                        List<int> blockedKeys = new List<int>(2);
                        blockedKeys.Add(KEY_SCANNING);
                        blockedKeys.Add(KEY_SAY);
                        LowLevelKeyboardHook.Instance.SetBlockedKeys(blockedKeys);

                        // Subscribe to low level keypress events
                        LowLevelKeyboardHook.Instance.KeyDownEvent += new LowLevelKeyboardHook.KeyboardEventHandler(Instance_KeyDownEvent);
                        LowLevelKeyboardHook.Instance.KeyUpEvent += new LowLevelKeyboardHook.KeyboardEventHandler(Instance_KeyUpEvent);

                        // Also set focus back to this
                        this.Focus();
                    });
                };
                timer.Enabled = true;
            }
            catch (Exception ex)
            {
                Log("Error attaching keyboard hook\t" + ex.Message);
            }
        }

        void Instance_KeyDownEvent(int keycode)
        {
            Log("Keypress\t" + keycode);
            switch (keycode)
            {
                case KEY_SCANNING:
                    ToggleFunctions();
                    break;
                case KEY_SAY:
                    keySayDown = true;
                    switch (currentFunction)
	                {
                        case ScanningFunctions.NoScanning:
                            break;
                        case ScanningFunctions.Up:
                            Log("Keypress\tSAY\tUP");
                            inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.UP);
                            break;
                        case ScanningFunctions.Down:
                            Log("Keypress\tSAY\tDOWN");
                            inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.DOWN);
                            break;
                        case ScanningFunctions.Left:
                            Log("Keypress\tSAY\tLEFT");
                            inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.LEFT);
                            break;
                        case ScanningFunctions.Right:
                            Log("Keypress\tSAY\tRIGHT");
                            inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.RIGHT);
                            break;
                        case ScanningFunctions.Launch:
                            Log("Keypress\tSAY\tLAUNCH");
                            BlinkLaunchButton();
                            inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.RETURN);
                            break;
                        case ScanningFunctions.Quit:
                            break;
                        default:
                            break;
	                }
                    break;
                default:
                    break;
            }
        }

        void Instance_KeyUpEvent(int keycode)
        {
            switch (keycode)
            {
                case KEY_SAY:
                    inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.RETURN);
                    switch (currentFunction)
	                {
                        case ScanningFunctions.NoScanning:
                            break;
                        case ScanningFunctions.Up:
                            inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.UP);
                            break;
                        case ScanningFunctions.Down:
                            inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.DOWN);
                            break;
                        case ScanningFunctions.Left:
                            inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.LEFT);
                            break;
                        case ScanningFunctions.Right:
                            inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.RIGHT);
                            break;
                        case ScanningFunctions.Launch:
                            BlinkLaunchButton();
                            inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.RETURN);
                            break;
                        case ScanningFunctions.Quit:
                            Log("Keypress\tSAY\tEXIT");
                            Application.Current.Shutdown();
                            break;
                        default:
                            break;
	                }
                    keySayDown = false;
                    break;
                default:
                    break;
            }
        }

        private void ToggleFunctions()
        {
            if (keySayDown)
            {
                return;
            }

            switch (currentFunction)
            {
                case ScanningFunctions.NoScanning:
                    Log("Keypress\tSCANNING\tChange_Function_UP");
                    currentFunction = ScanningFunctions.Up;
                    break;
                case ScanningFunctions.Up:
                    Log("Keypress\tSCANNING\tChange_Function_DOWN");
                    currentFunction = ScanningFunctions.Down;
                    break;
                case ScanningFunctions.Down:
                    Log("Keypress\tSCANNING\tChange_Function_LEFT");
                    currentFunction = ScanningFunctions.Left;
                    break;
                case ScanningFunctions.Left:
                    Log("Keypress\tSCANNING\tChange_Function_RIGHT");
                    currentFunction = ScanningFunctions.Right;
                    break;
                case ScanningFunctions.Right:
                    Log("Keypress\tSCANNING\tChange_Function_LAUNCH");
                    currentFunction = ScanningFunctions.Launch;
                    break;
                case ScanningFunctions.Launch:
                    Log("Keypress\tSCANNING\tChange_Function_QUIT");
                    currentFunction = ScanningFunctions.Quit;
                    break;
                case ScanningFunctions.Quit:
                    Log("Keypress\tSCANNING\tChange_Function_UP");
                    currentFunction = ScanningFunctions.Up;
                    break;
                default:
                    break;
            }

            VisualizeFunctions();
        }

        private void VisualizeFunctions()
        {
            switch (currentFunction)
            {
                case ScanningFunctions.NoScanning:
                    btnQuit.ClearValue(Button.BackgroundProperty);
                    btnUp.ClearValue(Button.BackgroundProperty);
                    btnDown.ClearValue(Button.BackgroundProperty);
                    btnLeft.ClearValue(Button.BackgroundProperty);
                    btnRight.ClearValue(Button.BackgroundProperty);
                    btnLaunch.ClearValue(Button.BackgroundProperty);
                    break;
                case ScanningFunctions.Up:
                    btnQuit.ClearValue(Button.BackgroundProperty);
                    btnDown.ClearValue(Button.BackgroundProperty);
                    btnLeft.ClearValue(Button.BackgroundProperty);
                    btnRight.ClearValue(Button.BackgroundProperty);
                    btnLaunch.ClearValue(Button.BackgroundProperty);
                    btnUp.Background = Brushes.Red;
                    break;
                case ScanningFunctions.Down:
                    btnQuit.ClearValue(Button.BackgroundProperty);
                    btnUp.ClearValue(Button.BackgroundProperty);
                    btnLeft.ClearValue(Button.BackgroundProperty);
                    btnRight.ClearValue(Button.BackgroundProperty);
                    btnLaunch.ClearValue(Button.BackgroundProperty);
                    btnDown.Background = Brushes.Red;
                    break;
                case ScanningFunctions.Left:
                    btnQuit.ClearValue(Button.BackgroundProperty);
                    btnUp.ClearValue(Button.BackgroundProperty);
                    btnDown.ClearValue(Button.BackgroundProperty);
                    btnRight.ClearValue(Button.BackgroundProperty);
                    btnLaunch.ClearValue(Button.BackgroundProperty);
                    btnLeft.Background = Brushes.Red;
                    break;
                case ScanningFunctions.Right:
                    btnQuit.ClearValue(Button.BackgroundProperty);
                    btnUp.ClearValue(Button.BackgroundProperty);
                    btnDown.ClearValue(Button.BackgroundProperty);
                    btnLeft.ClearValue(Button.BackgroundProperty);
                    btnLaunch.ClearValue(Button.BackgroundProperty);
                    btnRight.Background = Brushes.Red;
                    break;
                case ScanningFunctions.Launch:
                    btnQuit.ClearValue(Button.BackgroundProperty);
                    btnUp.ClearValue(Button.BackgroundProperty);
                    btnDown.ClearValue(Button.BackgroundProperty);
                    btnLeft.ClearValue(Button.BackgroundProperty);
                    btnRight.ClearValue(Button.BackgroundProperty);

                    SolidColorBrush animatedBrush = new SolidColorBrush(SystemColors.ControlBrush.Color);
                    btnLaunch.Background = animatedBrush;
                    ColorAnimation glowAnimation = new ColorAnimation(SystemColors.ControlBrush.Color, Colors.Red, new Duration(TimeSpan.FromMilliseconds(1000)));
                    glowAnimation.AutoReverse = true;
                    glowAnimation.RepeatBehavior = RepeatBehavior.Forever;
                    animatedBrush.BeginAnimation(SolidColorBrush.ColorProperty, glowAnimation);

                    break;
                case ScanningFunctions.Quit:
                    btnUp.ClearValue(Button.BackgroundProperty);
                    btnDown.ClearValue(Button.BackgroundProperty);
                    btnLeft.ClearValue(Button.BackgroundProperty);
                    btnRight.ClearValue(Button.BackgroundProperty);
                    btnLaunch.ClearValue(Button.BackgroundProperty);
                    btnQuit.Background = Brushes.Red;
                    break;
                default:
                    break;
            }
        }

        private void BlinkLaunchButton() 
        {
            // Blink
            SolidColorBrush animatedBrush = new SolidColorBrush(SystemColors.ControlBrush.Color);
            btnLaunch.Background = animatedBrush;
            ColorAnimation blinkAnimation = new ColorAnimation(SystemColors.ControlBrush.Color, Colors.Red, new Duration(TimeSpan.FromMilliseconds(200)));
            blinkAnimation.AutoReverse = true;
            blinkAnimation.RepeatBehavior = RepeatBehavior.Forever;
            animatedBrush.BeginAnimation(SolidColorBrush.ColorProperty, blinkAnimation);

            // Back to glow
            Timer timer = new Timer(5000);
            timer.Elapsed += delegate
            {
                this.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (DummyDelegate)
                delegate
                {
                    timer.Enabled = false;
                    VisualizeFunctions();
                });
            };
            timer.Enabled = true;
        }

        #region Mouse functions
        private void btnUp_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Log("MouseClick\tUP");
            inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.UP);
        }

        private void btnUp_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.UP);
        }

        private void btnDown_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Log("MouseClick\tDOWN");
            inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.DOWN);
        }

        private void btnDown_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.DOWN);
        }

        private void btnLeft_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Log("MouseClick\tLEFT");
            inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.LEFT);
        }

        private void btnLeft_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.LEFT);
        }

        private void btnRight_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Log("MouseClick\tRIGHT");
            inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.RIGHT);
        }

        private void btnRight_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.RIGHT);
        }

        private void btnLaunch_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Log("MouseClick\tLAUNCH");
            inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.RETURN);
            BlinkLaunchButton();
        }

        private void btnLaunch_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            inputSimulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.RETURN);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Log("MouseClick\tWindow");
            Application.Current.Shutdown();
        }

        private void btnQuit_Click(object sender, RoutedEventArgs e)
        {
            Log("MouseClick\tQUIT");
            Application.Current.Shutdown();
        }
        #endregion

        private void Log(string text)
        {
            try
            {
                if (text != "")
                {
                    string baseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                    StreamWriter cachefile = File.AppendText(baseDir + "\\" + LOGFILE);
                    cachefile.WriteLine(DateTime.Now + "\t" + text);
                    cachefile.Close();
                }
            }
            catch (Exception) { }
        }
    }
}
