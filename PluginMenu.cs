﻿namespace EmergencyV
{
    // System
    using System;
    using System.Linq;
    using System.Collections.Generic;

    // RPH
    using Rage;
    using Rage.Native;

    internal class PluginMenu
    {
        private static PluginMenu instance;
        public static PluginMenu Instance
        {
            get
            {
                if (instance == null)
                    instance = new PluginMenu();
                return instance;
            }
        }
        
        public Dictionary<string, Menu> MenusByIdentifier = new Dictionary<string, Menu>();
        public Dictionary<string, MenuItem> ItemsByIdentifier = new Dictionary<string, MenuItem>();
        private PluginMenu()
        {
            AddMenu("MAIN_MENU");
            AddMenu("ACTIONS_SUBMENU");
            AddMenu("REQUEST_BACKUP_SUBMENU");

            AddItem("OPEN_ACTIONS_SUBMENU_ITEM", "MAIN_MENU", "Actions", null, null, "ACTIONS_SUBMENU");
            AddItem("OPEN_REQUEST_BACKUP_SUBMENU_ITEM", "MAIN_MENU", "Request Backup", null, null, "REQUEST_BACKUP_SUBMENU");

            AddItem("BACKUP_WIP_ITEM", "REQUEST_BACKUP_SUBMENU", "[WIP]", () => { Game.DisplaySubtitle("[WIP]"); });

            AddItem("BACKUP_SEND_FIRETRUCK_EXTINGUISH_FIRE_ITEM", "REQUEST_BACKUP_SUBMENU", "Firefighters - Extinguish Fire", () => { BackupFunctions.SendFirefightersUnit(Game.LocalPlayer.Character.Position, BackupFunctions.FirefighterBackupTask.ExtinguishFireInArea); });
            AddItem("BACKUP_SEND_AMBULANCE_ITEM", "REQUEST_BACKUP_SUBMENU", "Paramedics", () => { BackupFunctions.SendParamedicUnit(Game.LocalPlayer.Character.Position, BackupFunctions.ParamedicBackupTask.TODO); });
#if DEBUG
            AddItem("CREATE_FIRES_ITEM", "ACTIONS_SUBMENU", "[DEBUG] Create Fires", () => 
            {
                GameFiber.StartNew(() => 
                {
                    API.FireEx[] fires = API.Functions.CreateFires(Game.LocalPlayer.Character.GetOffsetPositionFront(5.0f), 3.5f, 10, 5, false, false, true);

                    GameFiber.Sleep(500000);

                    for (int i = 0; i < fires.Length; i++)
                    {
                        if(fires[i].Fire) fires[i].Fire.Delete();
                    }
                });
            });

            AddItem("CREATE_BIG_FIRES_ITEM", "ACTIONS_SUBMENU", "[DEBUG] Create Big Fires", () =>
            {
                GameFiber.StartNew(() =>
                {
                    API.FireEx[] fires = API.Functions.CreateFires(Game.LocalPlayer.Character.GetOffsetPositionFront(5.0f), 3.5f, 10, 5, false, true, true);

                    GameFiber.Sleep(500000);

                    for (int i = 0; i < fires.Length; i++)
                    {
                        if (fires[i].Fire) fires[i].Fire.Delete();
                    }
                });
            });
#endif
        }

        public void Update()
        {
            if (PlayerManager.Instance.PlayerState != PlayerStateType.Normal && Plugin.Controls["OPEN_MENU"].IsJustPressed())
                MenusByIdentifier["MAIN_MENU"].IsVisible = !MenusByIdentifier["MAIN_MENU"].IsVisible;
        }

        public bool IsMenuAdded(string uniqueIdentifier)
        {
            return MenusByIdentifier.ContainsKey(uniqueIdentifier);
        }

        public bool IsItemAdded(string uniqueIdentifier)
        {
            return ItemsByIdentifier.ContainsKey(uniqueIdentifier);
        }

        public void AddMenu(string uniqueIdentifier)
        {
            if (IsMenuAdded(uniqueIdentifier))
                throw new InvalidOperationException($"Cannot add menu with ID \"{uniqueIdentifier}\", it's already added.");

            Game.LogTrivial($"Adding menu - ID:{uniqueIdentifier}");

            Menu m = new Menu();
            MenusByIdentifier.Add(uniqueIdentifier, m);
        }

        public void RemoveMenu(string uniqueIdentifier)
        {
            if (!IsMenuAdded(uniqueIdentifier))
                throw new InvalidOperationException($"Cannot remove menu with ID \"{uniqueIdentifier}\", it wasn't added or it's already removed.");

            Game.LogTrivial($"Removing menu - ID:{uniqueIdentifier}");

            Menu m = MenusByIdentifier[uniqueIdentifier];
            if (m.ParentMenu != null && m.ParentMenu.OpenedSubmenu == m)
                m.ParentMenu.CloseSubmenu();
            m.Delete();
            List<string> keysToRemove = new List<string>();
            foreach (MenuItem item in m.Items)
                foreach (KeyValuePair<string, MenuItem> p in ItemsByIdentifier.Where(x => x.Value == item))
                    keysToRemove.Add(p.Key);

            foreach (string k in keysToRemove)
                RemoveItem(k);

            MenusByIdentifier.Remove(uniqueIdentifier);
        }

        public void AddItem(string uniqueIdentifier, string menuIdentifier, string text, Action callback = null, Control? shortcutControl = null, string submenuToBindIdentifier = null)
        {
            if (uniqueIdentifier == null)
                throw new ArgumentNullException(nameof(uniqueIdentifier));
            if (menuIdentifier == null)
                throw new ArgumentNullException(nameof(menuIdentifier));

            if (IsItemAdded(uniqueIdentifier))
                throw new InvalidOperationException($"Cannot add item with ID \"{uniqueIdentifier}\", it's already added.");

            Game.LogTrivial($"Adding menu item - ID:{uniqueIdentifier}");

            MenuItem item = new MenuItem(text, callback, shortcutControl);
            if (submenuToBindIdentifier != null)
            {
                if (!IsMenuAdded(submenuToBindIdentifier))
                    throw new InvalidOperationException($"Cannot bind item with ID \"{uniqueIdentifier}\" to menu with ID \"{submenuToBindIdentifier}\", the menu isn't added.");

                item.BindedSubmenu = MenusByIdentifier[submenuToBindIdentifier];
                item.BindedSubmenu.ParentMenu = MenusByIdentifier[menuIdentifier];
            }

            if (!IsMenuAdded(menuIdentifier))
                throw new InvalidOperationException($"Cannot add item with ID \"{uniqueIdentifier}\" to menu with ID \"{submenuToBindIdentifier}\", the menu isn't added.");

            MenusByIdentifier[menuIdentifier].Items.Add(item);
            ItemsByIdentifier.Add(uniqueIdentifier, item);
        }

        public void RemoveItem(string uniqueIdentifier)
        {
            if (!IsItemAdded(uniqueIdentifier))
                throw new InvalidOperationException($"Cannot remove item with ID \"{uniqueIdentifier}\", it wasn't added or it's already removed.");

            Game.LogTrivial($"Removing menu item - ID:{uniqueIdentifier}");

            MenuItem item = ItemsByIdentifier[uniqueIdentifier];

            Menu menu = MenusByIdentifier.FirstOrDefault(p => p.Value.Items.Contains(item)).Value;
            if (menu != null)
            {
                menu.Items.Remove(item);
                if (menu.SelectedItem == item)
                    menu.SelectedItem = null;
            }

            ItemsByIdentifier.Remove(uniqueIdentifier);
        }

        public void UpdateItem(string uniqueIdentifier, string text, Action callback = null, Control? shortcutControl = null, string submenuToBindIdentifier = null)
        {
            if (!IsItemAdded(uniqueIdentifier))
                throw new InvalidOperationException($"Cannot update item with ID \"{uniqueIdentifier}\", it wasn't added or it was removed.");

            MenuItem item = ItemsByIdentifier[uniqueIdentifier];

            item.Text = text;
            item.Callback = callback;
            item.ShortcutControl = shortcutControl;

            if (submenuToBindIdentifier != null)
            {
                if (!IsMenuAdded(submenuToBindIdentifier))
                    throw new InvalidOperationException($"Cannot bind item with ID \"{uniqueIdentifier}\" to menu with ID \"{submenuToBindIdentifier}\", the menu isn't added.");

                item.BindedSubmenu = MenusByIdentifier[submenuToBindIdentifier];
            }
        }

        public void UpdateItem(string uniqueIdentifier, string text)
        {
            if (!IsItemAdded(uniqueIdentifier))
                throw new InvalidOperationException($"Cannot update item with ID \"{uniqueIdentifier}\", it wasn't added or it was removed.");

            MenuItem item = ItemsByIdentifier[uniqueIdentifier];

            item.Text = text;
        }
    }
}
