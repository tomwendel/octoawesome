﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using MonoGameUi;
using OctoAwesome.Client.Controls;

namespace OctoAwesome.Client.Components
{
    public class InputManager
    {
        #region Singleton

        private static InputManager _instance;

        public static InputManager Instance
        {
            get
            {
                if(_instance == null)
                    _instance = new InputManager();
                return _instance;
            }
        }

        #endregion

        /// <summary>
        /// Bietet die Möglichkeit den InputManager zu deaktivieren
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        /// Die interne Liste von Bindings
        /// </summary>
        private Dictionary<string, KeyBinding> bindings;

        /// <summary>
        /// Die abrufbare Liste von Bindings
        /// </summary>
        public Dictionary<string,KeyBinding> Bindings { get { return bindings; } }


        private InputManager()
        {
            bindings = new Dictionary<string, KeyBinding>();

            //Set the ConsoleCommand up
            #region ConsoleCommand
            CommandManager.Instance.RegisterCommand("bindings", (string[] args) =>
            {
                
                if (args.Length == 1)
                {
                    if (args[0] == "list")
                    {
                        ConsoleControl.WriteLine("===============[Keybindings]===============");
                        foreach (KeyValuePair<string, KeyBinding> binding in bindings)
                        {
                            ConsoleControl.WriteLine(binding.Key + " - " + binding.Value.Key.ToString());
                        }
                        ConsoleControl.WriteLine("=========================================");
                    }
                    else if (args[0] == "help")
                    {
                        ConsoleControl.WriteLine("===============[Binding Help]===============");
                        ConsoleControl.WriteLine("- /bindings list - list bindings");
                        ConsoleControl.WriteLine("- /bindings set <name> <key> - set key");
                    }
                }
                else if (args.Length == 3)
                {
                    if (args[0] == "set")
                    {
                        Keys key;
                        if (!Keys.TryParse(args[2], true, out key))
                            ConsoleControl.WriteLine("Invalid Key: " + args[2]);

                        try
                        {
                            SetKey(args[1],key);
                            ConsoleControl.WriteLine(args[1] + " set to " + args[2]);
                        }
                        catch (Exception e) { ConsoleControl.WriteLine(e.Message);}
                    }
                }
                else
                    ConsoleControl.WriteLine("Invalid. Use /bindings help to get help.");
            });
            #endregion
        }

        /// <summary>
        /// Registriert ein neues Binding ohne Aktion -> diese kann später gesetzt werden
        /// </summary>
        /// <param name="name">interner name (id)</param>
        /// <param name="displayName">anzuzeigender name</param>
        /// <param name="key">taste</param>
        public void RegisterBinding(string name, string displayName, Keys key)
        {
            if(bindings.ContainsKey(name))
                throw new Exception("Binding already exists");

            KeyBinding newBinding = new KeyBinding(displayName, key, null);
            bindings.Add(name, newBinding);
        }

        /// <summary>
        /// Registriert ein neues Binding mit Aktion
        /// </summary>
        /// <param name="name">interner name (id)</param>
        /// <param name="displayName">anzuzeigender name</param>
        /// <param name="key">taste</param>
        /// <param name="action">aktion</param>
        public void RegisterBinding(string name, string displayName, Keys key, Action action)
        {
            if (bindings.ContainsKey(name))
                throw new Exception("Binding already exists");

            KeyBinding newBinding = new KeyBinding(displayName, key, action);
            bindings.Add(name, newBinding);
        }

        /// <summary>
        /// Setzt die Aktion eines Binding
        /// </summary>
        /// <param name="name">interner name (id) des bindings</param>
        /// <param name="action">aktion</param>
        public void SetAction(string name, Action action)
        {
            if (!bindings.ContainsKey(name))
                throw new Exception("No binding with name " + name);

            bindings[name].Action = action;
        }

        /// <summary>
        /// Setzt die Taste eines Bindings
        /// </summary>
        /// <param name="name">interner name (id) des bindings</param>
        /// <param name="key">aktion</param>
        public void SetKey(string name, Keys key)
        {
            if (!bindings.ContainsKey(name))
                throw new Exception("No binding with name " + name);

            bindings[name].Key = key;
        }

        /// <summary>
        /// Setzt den anzuzeigenden Namen eines Bindings
        /// </summary>
        /// <param name="name">interner name (id) des bindings</param>
        /// <param name="displayName">anzuzeigender name</param>
        public void SetDisplayName(string name, string displayName)
        {
            if (!bindings.ContainsKey(name))
                throw new Exception("No binding with name " + name);

            bindings[name].DisplayName = displayName;
        }

        /// <summary>
        /// Wird aufgerufen wenn eine Taste gedrückt und nicht vom UI Framework verarbeitet wurde
        /// </summary>
        /// <param name="args">Argumente</param>
        public void OnKeyDown(KeyEventArgs args)
        {
            if (Disabled) return;

            Keys key = args.Key;

            if (!bindings.Any(b => b.Value.Key == key))
                return;

            var matches = bindings.Where(b => b.Value.Key == key);

            foreach (KeyValuePair<string, KeyBinding> match in matches)
            {
                match.Value.Action?.Invoke();
            }
        }
    }

    public class KeyBinding
    {
        public string DisplayName { get; set; }
        public Keys Key { get; set; }
        public Action Action { get; set; }

        internal KeyBinding(string displayName, Keys key, Action action)
        {
            DisplayName = displayName;
            Key = key;
            Action = action;
        }
    }
}
