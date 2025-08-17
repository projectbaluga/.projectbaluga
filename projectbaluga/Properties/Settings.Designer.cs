namespace projectbaluga.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.11.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("http://10.0.0.1")]
        public string HotspotUrl {
            get {
                return ((string)(this["HotspotUrl"]));
            }
            set {
                this["HotspotUrl"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("http://bojex.computers/status")]
        public string PostLoginUrl {
            get {
                return ((string)(this["PostLoginUrl"]));
            }
            set {
                this["PostLoginUrl"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("http://bojex.computers/login")]
        public string LockScreenUrl {
            get {
                return ((string)(this["LockScreenUrl"]));
            }
            set {
                this["LockScreenUrl"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IsTopmost {
            get {
                return ((bool)(this["IsTopmost"]));
            }
            set {
                this["IsTopmost"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool EnableAutoShutdown {
            get {
                return ((bool)(this["EnableAutoShutdown"]));
            }
            set {
                this["EnableAutoShutdown"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool DisableActiveProbing {
            get {
                return ((bool)(this["DisableActiveProbing"]));
            }
            set {
                this["DisableActiveProbing"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public int ShutdownTimeoutMinutes {
            get {
                return ((int)(this["ShutdownTimeoutMinutes"]));
            }
            set {
                this["ShutdownTimeoutMinutes"] = value;
            }
        }
    }
}
