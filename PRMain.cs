using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace PlaceableRestrict
{
    public class PRPlayerDate
    {
        public string PlayerName;
        public Dictionary<float, List<uint>> FarminstanceID;
        public Dictionary<float, List<uint>> BarricadeinstanceID;
        public List<uint> StructureinstanceID;
        public List<uint> VehicleinstanceID;
    }
    public class PRMain : RocketPlugin<PRConfig>
    {
        
        public static PRMain Instance;
        public Dictionary<ulong, PRPlayerDate> PRPlayer;
        public Dictionary<BarricadeRegion, uint> IsInVehicle;
        protected override void Load()
        {
            Instance = this;
            PRPlayer = new Dictionary<ulong, PRPlayerDate>();
            IsInVehicle = new Dictionary<BarricadeRegion, uint>();
            Rocket.Core.Logging.Logger.Log("PlaceableRestrict has been loaded...");
            U.Events.OnPlayerConnected += OnPlayer_Connected;
            BarricadeManager.onDeployBarricadeRequested += Deploy_Barricade;
            BarricadeManager.onBarricadeSpawned += Barricade_Spawned;
            BarricadeManager.onDamageBarricadeRequested += Damage_Barricade;
            BarricadeDrop.OnSalvageRequested_Global += Salvage_Barricade;
            InteractableFarm.OnHarvestRequested_Global += Harvest_Plant;
            VehicleManager.OnToggledVehicleLock += Locked_Vehicle;
            StructureManager.onDeployStructureRequested += Deploy_Structure;
            StructureManager.onStructureSpawned += Structure_Spawned;
            StructureManager.onDamageStructureRequested += Damage_Structure;
            StructureDrop.OnSalvageRequested_Global += Salvage_Structure;
            PRJsonSave.Load();
        }
        protected override void Unload()
        {
            U.Events.OnPlayerConnected -= OnPlayer_Connected;
            BarricadeManager.onDeployBarricadeRequested -= Deploy_Barricade;
            BarricadeManager.onBarricadeSpawned -= Barricade_Spawned;
            BarricadeManager.onDamageBarricadeRequested -= Damage_Barricade;
            BarricadeDrop.OnSalvageRequested_Global -= Salvage_Barricade;
            InteractableFarm.OnHarvestRequested_Global -= Harvest_Plant;
            VehicleManager.OnToggledVehicleLock += Locked_Vehicle;
            StructureManager.onDeployStructureRequested -= Deploy_Structure;
            StructureManager.onStructureSpawned -= Structure_Spawned;
            StructureManager.onDamageStructureRequested -= Damage_Structure;
            StructureDrop.OnSalvageRequested_Global -= Salvage_Structure;
            PRJsonSave.Save(PRPlayer);
        }

        private void Locked_Vehicle(InteractableVehicle vehicle)
        {
            if (PRMain.Instance.Configuration.Instance.VehicleLock_MaxNumber == 0)
                return;
            if (!vehicle.isLocked)
            {
                if(PRPlayer[(ulong)vehicle.lockedOwner].VehicleinstanceID.Contains(vehicle.instanceID))
                {
                    PRPlayer[(ulong)vehicle.lockedOwner].VehicleinstanceID.Remove(vehicle.instanceID);
                }
                return;
            }
            List<uint> NewPlayrVehicleDate = new List<uint>();
            foreach (var item in PRPlayer[(ulong)vehicle.lockedOwner].VehicleinstanceID)
            {
                if(VehicleManager.findVehicleByNetInstanceID(item) != null && VehicleManager.findVehicleByNetInstanceID(item).health != 0 && VehicleManager.findVehicleByNetInstanceID(item).lockedOwner == vehicle.lockedOwner)
                {
                    NewPlayrVehicleDate.Add(item);
                }
            }
            PRPlayer[(ulong)vehicle.lockedOwner].VehicleinstanceID = NewPlayrVehicleDate;
            UnturnedPlayer unplaye = UnturnedPlayer.FromCSteamID(vehicle.lockedOwner);
            int VehicleNumber = PRPlayer[(ulong)vehicle.lockedOwner].VehicleinstanceID.Count + 1;
            if (VehicleNumber > PRMain.Instance.Configuration.Instance.VehicleLock_MaxNumber)
            {
                VehicleManager.unlockVehicle(vehicle, unplaye.Player);
                unplaye.Player.ServerShowHint(Translate("Command_VehcleLose", PRMain.Instance.Configuration.Instance.VehicleLock_MaxNumber), PRMain.Instance.Configuration.Instance.Hint_Time);
                UnturnedChat.Say(VehicleNumber.ToString());
            }
            else
            {
                if (!PRPlayer[(ulong)vehicle.lockedOwner].VehicleinstanceID.Contains(vehicle.instanceID))
                {
                    PRPlayer[(ulong)vehicle.lockedOwner].VehicleinstanceID.Add(vehicle.instanceID);
                }
                unplaye.Player.ServerShowHint(Translate("Command_AddVehicle", PRPlayer[(ulong)vehicle.lockedOwner].VehicleinstanceID.Count, PRMain.Instance.Configuration.Instance.VehicleLock_MaxNumber), PRMain.Instance.Configuration.Instance.Hint_Time);
            }
        }
        private void Deploy_Structure(Structure structure, ItemStructureAsset asset, ref Vector3 point, ref float angle_x, ref float angle_y, ref float angle_z, ref ulong owner, ref ulong group, ref bool shouldAllow)
        {
            if (PRMain.Instance.Configuration.Instance.PRItem.Contains(asset.id))
            {
                shouldAllow = false;
                UnturnedPlayer.FromCSteamID((CSteamID)owner).Player.ServerShowHint(Translate("Command_PRLose"), PRMain.Instance.Configuration.Instance.Hint_Time);
                return;
            }
            if (PRMain.Instance.Configuration.Instance.Structure_MaxNumber == 0)
                return;
            if (PRPlayer[owner].StructureinstanceID.Count >= PRMain.Instance.Configuration.Instance.Structure_MaxNumber)
            {
                UnturnedPlayer.FromCSteamID((CSteamID)owner).Player.ServerShowHint(Translate("Command_Lose", PRMain.Instance.Configuration.Instance.Structure_MaxNumber), PRMain.Instance.Configuration.Instance.Hint_Time);
                shouldAllow = false;
                return;
            }
            else
            {
                shouldAllow = true;
                return;
            }
        }
        private void Structure_Spawned(StructureRegion region, StructureDrop drop)
        {
            if (drop.GetServersideData().owner == 0)
                return;
            if (!PRPlayer[drop.GetServersideData().owner].StructureinstanceID.Contains(drop.instanceID))
            {
                PRPlayer[drop.GetServersideData().owner].StructureinstanceID.Add(drop.instanceID);
            }
            int StructureNumber = PRPlayer[drop.GetServersideData().owner].StructureinstanceID.Count;
            UnturnedPlayer.FromCSteamID((CSteamID)drop.GetServersideData().owner).Player.ServerShowHint(Translate("Command_Add", StructureNumber, PRMain.Instance.Configuration.Instance.Structure_MaxNumber), PRMain.Instance.Configuration.Instance.Hint_Time);

        }
        private void Damage_Structure(CSteamID instigatorSteamID, Transform structureTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
            if (pendingTotalDamage >= StructureManager.FindStructureByRootTransform(structureTransform).GetServersideData().structure.health)
            {
                if(PRPlayer[StructureManager.FindStructureByRootTransform(structureTransform).GetServersideData().owner].StructureinstanceID.Contains(StructureManager.FindStructureByRootTransform(structureTransform).instanceID))
                {
                    PRPlayer[StructureManager.FindStructureByRootTransform(structureTransform).GetServersideData().owner].StructureinstanceID.Remove(StructureManager.FindStructureByRootTransform(structureTransform).instanceID);
                }
            }
        }
        private void Salvage_Structure(StructureDrop structure, SteamPlayer instigatorClient, ref bool shouldAllow)
        {
            if(PRPlayer[structure.GetServersideData().owner].StructureinstanceID.Contains(structure.instanceID))
            { 
                PRPlayer[structure.GetServersideData().owner].StructureinstanceID.Remove(structure.instanceID); 
            }
        }
        private void Harvest_Plant(InteractableFarm harvestable, SteamPlayer instigatorPlayer, ref bool shouldAllow)
        {
            if(PRMain.Instance.Configuration.Instance.Farm_Protection)
            {
                if (instigatorPlayer.playerID.steamID != (CSteamID)BarricadeManager.FindBarricadeByRootTransform(harvestable.transform).GetServersideData().owner)
                {
                    shouldAllow = false;
                    UnturnedPlayer.FromCSteamID(instigatorPlayer.playerID.steamID).Player.ServerShowHint(Translate("Command_FarmLose"), PRMain.Instance.Configuration.Instance.Hint_Time);
                }
            }
        }
        private void Deploy_Barricade(Barricade barricade, ItemBarricadeAsset asset, Transform hit, ref Vector3 point, ref float angle_x, ref float angle_y, ref float angle_z, ref ulong owner, ref ulong group, ref bool shouldAllow)
        {
            if(PRMain.Instance.Configuration.Instance.PRItem.Contains(asset.id))
            {
                shouldAllow = false;
                UnturnedPlayer.FromCSteamID((CSteamID)owner).Player.ServerShowHint(Translate("Command_PRLose"), PRMain.Instance.Configuration.Instance.Hint_Time);
                return;
            }
            if (asset.type == EItemType.FARM)
            {
                if (PRMain.Instance.Configuration.Instance.Farm_MaxNumber == 0)
                    return;
                Dictionary<float, List<uint>> NewFarminstanceID = new Dictionary<float, List<uint>>();
                foreach (var item in PRPlayer[owner].FarminstanceID)
                {
                    if(item.Key != 0.1f)
                    {
                        if (VehicleManager.findVehicleByNetInstanceID((uint)item.Key) != null && VehicleManager.findVehicleByNetInstanceID((uint)item.Key).health != 0)
                        {
                            NewFarminstanceID.Add(item.Key, item.Value);
                        }
                    }                    
                }
                NewFarminstanceID.Add(0.1f, PRPlayer[owner].FarminstanceID[0.1f]);
                PRPlayer[owner].FarminstanceID = NewFarminstanceID;
                int FarmNumber = 0;
                foreach (var item in NewFarminstanceID)
                {
                    FarmNumber += item.Value.Count;
                }
                if (FarmNumber >= PRMain.Instance.Configuration.Instance.Farm_MaxNumber)
                {
                    UnturnedPlayer.FromCSteamID((CSteamID)owner).Player.ServerShowHint(Translate("Command_Lose", PRMain.Instance.Configuration.Instance.Farm_MaxNumber), PRMain.Instance.Configuration.Instance.Hint_Time);
                    shouldAllow = false;
                    return;
                }
                else
                {
                    shouldAllow = true;
                    if (hit != null && hit.transform.CompareTag("Vehicle"))
                    {
                        InteractableVehicle vehicle = hit.GetComponent<InteractableVehicle>();
                        if(!IsInVehicle.ContainsKey(BarricadeManager.findRegionFromVehicle(vehicle)))
                        {
                            IsInVehicle.Add(BarricadeManager.findRegionFromVehicle(vehicle), vehicle.instanceID);
                        }
                    }
                    return;
                }
            }
            else
            {
                if (PRMain.Instance.Configuration.Instance.Barricade_MaxNumber == 0)
                    return;
                Dictionary<float, List<uint>> NewBarricadeinstanceID = new Dictionary<float, List<uint>>();
                foreach (var item in PRPlayer[owner].BarricadeinstanceID)
                {
                    if(item.Key != 0.1f)
                    {
                        if (VehicleManager.findVehicleByNetInstanceID((uint)item.Key) != null && VehicleManager.findVehicleByNetInstanceID((uint)item.Key).health != 0)
                        {
                            NewBarricadeinstanceID.Add(item.Key, item.Value);
                        }
                    }
                }
                NewBarricadeinstanceID.Add(0.1f, PRPlayer[owner].BarricadeinstanceID[0.1f]);
                PRPlayer[owner].FarminstanceID = NewBarricadeinstanceID;
                int BarricadeNumber = 0;
                foreach (var item in NewBarricadeinstanceID)
                {
                    BarricadeNumber += item.Value.Count;
                }
                if (BarricadeNumber >= PRMain.Instance.Configuration.Instance.Barricade_MaxNumber)
                {
                    UnturnedPlayer.FromCSteamID((CSteamID)owner).Player.ServerShowHint(Translate("Command_Lose", PRMain.Instance.Configuration.Instance.Barricade_MaxNumber), PRMain.Instance.Configuration.Instance.Hint_Time);
                    shouldAllow = false;
                    return;
                }
                else
                {
                    shouldAllow = true;
                    if (hit != null && hit.transform.CompareTag("Vehicle"))
                    {
                        InteractableVehicle vehicle = hit.GetComponent<InteractableVehicle>();
                        if (!IsInVehicle.ContainsKey(BarricadeManager.findRegionFromVehicle(vehicle)))
                        {
                            IsInVehicle.Add(BarricadeManager.findRegionFromVehicle(vehicle), vehicle.instanceID);
                        }
                    }
                    return;
                }
            }
        }
        private void Barricade_Spawned(BarricadeRegion region, BarricadeDrop drop)
        {
            
            if (drop.GetServersideData().owner == 0)
                return;
            if (drop.asset.type != EItemType.FARM)
            {
                if (IsInVehicle.ContainsKey(region))
                {
                    if(!PRPlayer[drop.GetServersideData().owner].BarricadeinstanceID.ContainsKey(IsInVehicle[region]))
                    {
                        List<uint> AddBarricadeinstanceID = new List<uint>
                        {
                            drop.instanceID
                        };
                        PRPlayer[drop.GetServersideData().owner].BarricadeinstanceID.Add(IsInVehicle[region], AddBarricadeinstanceID);
                    }
                    else
                    {
                        PRPlayer[drop.GetServersideData().owner].BarricadeinstanceID[IsInVehicle[region]].Add(drop.instanceID);
                    }
                }
                else
                {
                    PRPlayer[drop.GetServersideData().owner].BarricadeinstanceID[0.1f].Add(drop.instanceID);
                }
                int BarricadeNumber = 0;
                foreach (var item in PRPlayer[drop.GetServersideData().owner].BarricadeinstanceID)
                {
                    BarricadeNumber += item.Value.Count;
                }
                UnturnedPlayer.FromCSteamID((CSteamID)drop.GetServersideData().owner).Player.ServerShowHint(Translate("Command_Add", BarricadeNumber, PRMain.Instance.Configuration.Instance.Farm_MaxNumber), PRMain.Instance.Configuration.Instance.Hint_Time);
            }
            else
            {
                if (IsInVehicle.ContainsKey(region))
                {
                    if (!PRPlayer[drop.GetServersideData().owner].FarminstanceID.ContainsKey(IsInVehicle[region]))
                    {
                        List<uint> AddFarminstanceID = new List<uint>
                        {
                            drop.instanceID
                        };
                        PRPlayer[drop.GetServersideData().owner].FarminstanceID.Add(IsInVehicle[region], AddFarminstanceID);
                    }
                    else
                    {
                        PRPlayer[drop.GetServersideData().owner].BarricadeinstanceID[IsInVehicle[region]].Add(drop.instanceID);
                    }
                }
                else
                {
                    PRPlayer[drop.GetServersideData().owner].FarminstanceID[0.1f].Add(drop.instanceID);
                }
                int FarmNumber = 0;
                foreach (var item in PRPlayer[drop.GetServersideData().owner].FarminstanceID)
                {
                    FarmNumber += item.Value.Count;
                }
                UnturnedPlayer.FromCSteamID((CSteamID)drop.GetServersideData().owner).Player.ServerShowHint(Translate("Command_Add", FarmNumber, PRMain.Instance.Configuration.Instance.Farm_MaxNumber), PRMain.Instance.Configuration.Instance.Hint_Time);
            }
        }
        private void Damage_Barricade(CSteamID instigatorSteamID, Transform barricadeTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
            if (pendingTotalDamage >= BarricadeManager.FindBarricadeByRootTransform(barricadeTransform).GetServersideData().barricade.health)
            {
                if (BarricadeManager.FindBarricadeByRootTransform(barricadeTransform).asset.type != EItemType.FARM)
                {
                    if (PRPlayer[BarricadeManager.FindBarricadeByRootTransform(barricadeTransform).GetServersideData().owner].BarricadeinstanceID[0.1f].Contains(BarricadeManager.FindBarricadeByRootTransform(barricadeTransform).instanceID))
                    {
                        PRPlayer[BarricadeManager.FindBarricadeByRootTransform(barricadeTransform).GetServersideData().owner].BarricadeinstanceID.Remove(BarricadeManager.FindBarricadeByRootTransform(barricadeTransform).instanceID);
                    }
                }
                else
                {
                    if(PRPlayer[BarricadeManager.FindBarricadeByRootTransform(barricadeTransform).GetServersideData().owner].FarminstanceID[0.1f].Contains(BarricadeManager.FindBarricadeByRootTransform(barricadeTransform).instanceID))
                    {
                        PRPlayer[BarricadeManager.FindBarricadeByRootTransform(barricadeTransform).GetServersideData().owner].FarminstanceID.Remove(BarricadeManager.FindBarricadeByRootTransform(barricadeTransform).instanceID);
                    }
                }
            }
        }
        private void Salvage_Barricade(BarricadeDrop barricade, SteamPlayer instigatorClient, ref bool shouldAllow)
        {
            if (barricade.asset.type != EItemType.FARM)
            {
                if (PRPlayer[barricade.GetServersideData().owner].BarricadeinstanceID[0.1f].Contains(barricade.instanceID))
                {
                    PRPlayer[barricade.GetServersideData().owner].BarricadeinstanceID[0.1f].Remove(barricade.instanceID);
                }
            }
            else
            {
                if (PRPlayer[barricade.GetServersideData().owner].FarminstanceID[0.1f].Contains(barricade.instanceID))
                {
                    PRPlayer[barricade.GetServersideData().owner].FarminstanceID[0.1f].Remove(barricade.instanceID);
                }
            }
        }
        private void OnPlayer_Connected(UnturnedPlayer player)
        {
            if (!PRPlayer.ContainsKey((ulong)player.CSteamID))
            {
                Dictionary<float, List<uint>> Newplayer = new Dictionary<float, List<uint>>
                {
                    { 0.1f, new List<uint>() }
                };
                PRPlayer.Add((ulong)player.CSteamID, new PRPlayerDate() { PlayerName = player.CharacterName,BarricadeinstanceID = Newplayer, FarminstanceID = Newplayer, StructureinstanceID = new List<uint>(),VehicleinstanceID = new List<uint>()});
            }
            else
            {
                if (PRPlayer[(ulong)player.CSteamID].PlayerName != player.CharacterName)
                {
                    PRPlayer[(ulong)player.CSteamID].PlayerName = player.CharacterName;
                }
            }
        }
        public override TranslationList DefaultTranslations
        {
            get
            {
                return new TranslationList()
                {
                    {"Command_AddVehicle","上锁成功，限制 {0} / {1}。"},
                    {"Command_Add","放置成功，限制 {0} / {1}。"},
                    {"Command_Lose","放置失败，你的放置物品数量已达到上限，上限 {0}。"},
                    {"Command_PRLose","放置失败，该物品不允许放置。"},
                    {"Command_VehcleLose","上锁失败，你的载具上锁数量已达到上限，上限 {0}。"},
                    {"Command_FarmLose","收获失败，这不是你的种植物。"},
                    {"Command_Help1","输入/PRPRemove 类型 玩家名部分字符/steamid移除相应玩家的已放置/上锁物品记录。"},
                    {"Command_Help2","/PRPRemove v 载具上锁记录，/PRPRemove s 建筑放置记录，/PRPRemove b 非植物放置记录，/PRPRemove f 植物放置记录"},
                    {"Command_NotPlayer","未找到玩家 {0} 数据。"},
                    {"Command_REVPlayer","成功移除玩家 {0} 所有已上锁载具数据。"},
                    {"Command_RESPlayer","成功移除玩家 {0} 所有已放置建筑数据。"},
                    {"Command_REBPlayer","成功移除玩家 {0} 所有已放置障碍数据。"},
                    {"Command_REFPlayer","成功移除玩家 {0} 所有已放置植物数据。"},
                };
            }
        }
    }
    public class PRConfig : IRocketPluginConfiguration, IDefaultable
    {
        [XmlElement("最大种植物放置数")]
        public int Farm_MaxNumber;
        [XmlElement("种植物保护")]
        public bool Farm_Protection;
        [XmlElement("最大非植物障碍放置数")]
        public int Barricade_MaxNumber;
        [XmlElement("最大结构放置数")]
        public int Structure_MaxNumber;
        [XmlElement("最大载具上锁数")]
        public int VehicleLock_MaxNumber;
        [XmlElement("提示时间")]
        public int Hint_Time;
        [XmlElement("禁止放置物品ID")]
        public List<ushort> PRItem = new List<ushort>();
        public void LoadDefaults()
        {
            PRItem = new List<ushort>
            {
                383,384,385,
            };
            Farm_MaxNumber = 50;
            Farm_Protection = false;
            Barricade_MaxNumber = 500;
            Structure_MaxNumber = 500;
            VehicleLock_MaxNumber = 5;
            Hint_Time = 3;
        }
    }
    public class CommandRemoveDate : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "prpremove";

        public string Help => "";

        public string Syntax => "";

        public List<string> Aliases => new List<string> { };

        public List<string> Permissions => new List<string> { };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            if(player!= null)
            {
                if (command.Length == 0)
                {
                    UnturnedChat.Say(player.CSteamID, PRMain.Instance.Translate("Command_Help1"));
                    UnturnedChat.Say(player.CSteamID, PRMain.Instance.Translate("Command_Help2"));
                    return;
                }
                if (command.Length == 2 && command[0] == "v")
                {
                    bool IsPlayerID = ulong.TryParse(command[1], out ulong PlayerID);
                    if (IsPlayerID)
                    {
                        if (!PRMain.Instance.PRPlayer.ContainsKey(PlayerID))
                        {
                            UnturnedChat.Say(player.CSteamID, PRMain.Instance.Translate("Command_NotPlayer", command[1]));
                            return;
                        }
                        List<uint> NewVehicle = new List<uint>();
                        PRMain.Instance.PRPlayer[(ulong)player.CSteamID].VehicleinstanceID = NewVehicle;
                        UnturnedChat.Say(player.CSteamID, PRMain.Instance.Translate("Command_REVPlayer", PRMain.Instance.PRPlayer[(ulong)player.CSteamID].PlayerName));
                    }
                    else
                    {
                        PlayerID = PlayerCSteamIDFromName(command[1]);
                        if (PlayerID == 0)
                        {
                            UnturnedChat.Say(player.CSteamID, PRMain.Instance.Translate("Command_NotPlayer", command[1]));
                            return;
                        }
                        if (!PRMain.Instance.PRPlayer.ContainsKey(PlayerID))
                        {
                            UnturnedChat.Say(player.CSteamID, PRMain.Instance.Translate("Command_NotPlayer", command[1]));
                            return;
                        }
                        List<uint> NewVehicle = new List<uint>();
                        PRMain.Instance.PRPlayer[(ulong)player.CSteamID].VehicleinstanceID = NewVehicle;
                        UnturnedChat.Say(player.CSteamID, PRMain.Instance.Translate("Command_REVPlayer", PRMain.Instance.PRPlayer[(ulong)player.CSteamID].PlayerName));
                    }
                }
                if (command.Length == 2 && command[0] == "s")
                {
                    bool IsPlayerID = ulong.TryParse(command[1], out ulong PlayerID);
                    if (IsPlayerID)
                    {
                        if (!PRMain.Instance.PRPlayer.ContainsKey(PlayerID))
                        {
                            UnturnedChat.Say(player.CSteamID, PRMain.Instance.Translate("Command_NotPlayer", command[1]));
                            return;
                        }
                        List<uint> NewStructure = new List<uint>();
                        PRMain.Instance.PRPlayer[(ulong)player.CSteamID].StructureinstanceID = NewStructure;
                        UnturnedChat.Say(player.CSteamID, PRMain.Instance.Translate("Command_RESPlayer", PRMain.Instance.PRPlayer[(ulong)player.CSteamID].PlayerName));
                    }
                    else
                    {
                        PlayerID = PlayerCSteamIDFromName(command[1]);
                        if (PlayerID == 0)
                        {
                            UnturnedChat.Say(player.CSteamID, PRMain.Instance.Translate("Command_NotPlayer", command[1]));
                            return;
                        }
                        if (!PRMain.Instance.PRPlayer.ContainsKey(PlayerID))
                        {
                            UnturnedChat.Say(player.CSteamID, PRMain.Instance.Translate("Command_NotPlayer", command[1]));
                            return;
                        }
                        List<uint> NewVehicle = new List<uint>();
                        PRMain.Instance.PRPlayer[(ulong)player.CSteamID].VehicleinstanceID = NewVehicle;
                        UnturnedChat.Say(player.CSteamID, PRMain.Instance.Translate("Command_RESPlayer", PRMain.Instance.PRPlayer[(ulong)player.CSteamID].PlayerName));
                    }
                }
                if (command.Length == 2 && command[0] == "b")
                {
                    bool IsPlayerID = ulong.TryParse(command[1], out ulong PlayerID);
                    if (IsPlayerID)
                    {
                        if (!PRMain.Instance.PRPlayer.ContainsKey(PlayerID))
                        {
                            UnturnedChat.Say(player.CSteamID, PRMain.Instance.Translate("Command_NotPlayer", command[1]));
                            return;
                        }
                        Dictionary<float, List<uint>> NewBarricade = new Dictionary<float, List<uint>>
                    {
                        { 0.1f, new List<uint>() }
                    };
                        PRMain.Instance.PRPlayer[(ulong)player.CSteamID].BarricadeinstanceID = NewBarricade;
                        UnturnedChat.Say(player.CSteamID, PRMain.Instance.Translate("Command_RESPlayer", PRMain.Instance.PRPlayer[(ulong)player.CSteamID].PlayerName));
                    }
                    else
                    {
                        PlayerID = PlayerCSteamIDFromName(command[1]);
                        if (PlayerID == 0)
                        {
                            UnturnedChat.Say(player.CSteamID, PRMain.Instance.Translate("Command_NotPlayer", command[1]));
                            return;
                        }
                        if (!PRMain.Instance.PRPlayer.ContainsKey(PlayerID))
                        {
                            UnturnedChat.Say(player.CSteamID, PRMain.Instance.Translate("Command_NotPlayer", command[1]));
                            return;
                        }
                        Dictionary<float, List<uint>> NewBarricade = new Dictionary<float, List<uint>>
                    {
                        { 0.1f, new List<uint>() }
                    };
                        PRMain.Instance.PRPlayer[(ulong)player.CSteamID].BarricadeinstanceID = NewBarricade;
                        UnturnedChat.Say(player.CSteamID, PRMain.Instance.Translate("Command_RESPlayer", PRMain.Instance.PRPlayer[(ulong)player.CSteamID].PlayerName));
                    }
                }
                if (command.Length == 2 && command[0] == "f")
                {
                    bool IsPlayerID = ulong.TryParse(command[1], out ulong PlayerID);
                    if (IsPlayerID)
                    {
                        if (!PRMain.Instance.PRPlayer.ContainsKey(PlayerID))
                        {
                            UnturnedChat.Say(player.CSteamID, PRMain.Instance.Translate("Command_NotPlayer", command[1]));
                            return;
                        }
                        Dictionary<float, List<uint>> NewFarm = new Dictionary<float, List<uint>>
                    {
                        { 0.1f, new List<uint>() }
                    };
                        PRMain.Instance.PRPlayer[(ulong)player.CSteamID].FarminstanceID = NewFarm;
                        UnturnedChat.Say(player.CSteamID, PRMain.Instance.Translate("Command_RESPlayer", PRMain.Instance.PRPlayer[(ulong)player.CSteamID].PlayerName));
                    }
                    else
                    {
                        PlayerID = PlayerCSteamIDFromName(command[1]);
                        if (PlayerID == 0)
                        {
                            UnturnedChat.Say(player.CSteamID, PRMain.Instance.Translate("Command_NotPlayer", command[1]));
                            return;
                        }
                        if (!PRMain.Instance.PRPlayer.ContainsKey(PlayerID))
                        {
                            UnturnedChat.Say(player.CSteamID, PRMain.Instance.Translate("Command_NotPlayer", command[1]));
                            return;
                        }
                        Dictionary<float, List<uint>> NewFarm = new Dictionary<float, List<uint>>
                    {
                        { 0.1f, new List<uint>() }
                    };
                        PRMain.Instance.PRPlayer[(ulong)player.CSteamID].FarminstanceID = NewFarm;
                        UnturnedChat.Say(player.CSteamID, PRMain.Instance.Translate("Command_RESPlayer", PRMain.Instance.PRPlayer[(ulong)player.CSteamID].PlayerName));
                    }
                }
            }
            else
            {
                if (command.Length == 2 && command[0] == "v")
                {
                    bool IsPlayerID = ulong.TryParse(command[1], out ulong PlayerID);
                    if (IsPlayerID)
                    {
                        if (!PRMain.Instance.PRPlayer.ContainsKey(PlayerID))
                        {
                            Rocket.Core.Logging.Logger.Log(PRMain.Instance.Translate("Command_NotPlayer", command[1]));
                            return;
                        }
                        List<uint> NewVehicle = new List<uint>();
                        PRMain.Instance.PRPlayer[(ulong)player.CSteamID].VehicleinstanceID = NewVehicle;
                        Rocket.Core.Logging.Logger.Log(PRMain.Instance.Translate("Command_REVPlayer", PRMain.Instance.PRPlayer[(ulong)player.CSteamID].PlayerName));
                    }
                    else
                    {
                        PlayerID = PlayerCSteamIDFromName(command[1]);
                        if (PlayerID == 0)
                        {
                            Rocket.Core.Logging.Logger.Log(PRMain.Instance.Translate("Command_NotPlayer", command[1]));
                            return;
                        }
                        if (!PRMain.Instance.PRPlayer.ContainsKey(PlayerID))
                        {
                            Rocket.Core.Logging.Logger.Log(PRMain.Instance.Translate("Command_NotPlayer", command[1]));
                            return;
                        }
                        List<uint> NewVehicle = new List<uint>();
                        PRMain.Instance.PRPlayer[(ulong)player.CSteamID].VehicleinstanceID = NewVehicle;
                        Rocket.Core.Logging.Logger.Log(PRMain.Instance.Translate("Command_REVPlayer", PRMain.Instance.PRPlayer[(ulong)player.CSteamID].PlayerName));
                    }
                }
                if (command.Length == 2 && command[0] == "s")
                {
                    bool IsPlayerID = ulong.TryParse(command[1], out ulong PlayerID);
                    if (IsPlayerID)
                    {
                        if (!PRMain.Instance.PRPlayer.ContainsKey(PlayerID))
                        {
                            Rocket.Core.Logging.Logger.Log(PRMain.Instance.Translate("Command_NotPlayer", command[1]));
                            return;
                        }
                        List<uint> NewStructure = new List<uint>();
                        PRMain.Instance.PRPlayer[(ulong)player.CSteamID].StructureinstanceID = NewStructure;
                        Rocket.Core.Logging.Logger.Log(PRMain.Instance.Translate("Command_RESPlayer", PRMain.Instance.PRPlayer[(ulong)player.CSteamID].PlayerName));
                    }
                    else
                    {
                        PlayerID = PlayerCSteamIDFromName(command[1]);
                        if (PlayerID == 0)
                        {
                            Rocket.Core.Logging.Logger.Log(PRMain.Instance.Translate("Command_NotPlayer", command[1]));
                            return;
                        }
                        if (!PRMain.Instance.PRPlayer.ContainsKey(PlayerID))
                        {
                            Rocket.Core.Logging.Logger.Log(PRMain.Instance.Translate("Command_NotPlayer", command[1]));
                            return;
                        }
                        List<uint> NewVehicle = new List<uint>();
                        PRMain.Instance.PRPlayer[(ulong)player.CSteamID].VehicleinstanceID = NewVehicle;
                        Rocket.Core.Logging.Logger.Log(PRMain.Instance.Translate("Command_RESPlayer", PRMain.Instance.PRPlayer[(ulong)player.CSteamID].PlayerName));
                    }
                }
                if (command.Length == 2 && command[0] == "b")
                {
                    bool IsPlayerID = ulong.TryParse(command[1], out ulong PlayerID);
                    if (IsPlayerID)
                    {
                        if (!PRMain.Instance.PRPlayer.ContainsKey(PlayerID))
                        {
                            Rocket.Core.Logging.Logger.Log(PRMain.Instance.Translate("Command_NotPlayer", command[1]));
                            return;
                        }
                        Dictionary<float, List<uint>> NewBarricade = new Dictionary<float, List<uint>>
                    {
                        { 0.1f, new List<uint>() }
                    };
                        PRMain.Instance.PRPlayer[(ulong)player.CSteamID].BarricadeinstanceID = NewBarricade;
                        Rocket.Core.Logging.Logger.Log(PRMain.Instance.Translate("Command_RESPlayer", PRMain.Instance.PRPlayer[(ulong)player.CSteamID].PlayerName));
                    }
                    else
                    {
                        PlayerID = PlayerCSteamIDFromName(command[1]);
                        if (PlayerID == 0)
                        {
                            Rocket.Core.Logging.Logger.Log(PRMain.Instance.Translate("Command_NotPlayer", command[1]));
                            return;
                        }
                        if (!PRMain.Instance.PRPlayer.ContainsKey(PlayerID))
                        {
                            Rocket.Core.Logging.Logger.Log(PRMain.Instance.Translate("Command_NotPlayer", command[1]));
                            return;
                        }
                        Dictionary<float, List<uint>> NewBarricade = new Dictionary<float, List<uint>>
                    {
                        { 0.1f, new List<uint>() }
                    };
                        PRMain.Instance.PRPlayer[(ulong)player.CSteamID].BarricadeinstanceID = NewBarricade;
                        Rocket.Core.Logging.Logger.Log(PRMain.Instance.Translate("Command_RESPlayer", PRMain.Instance.PRPlayer[(ulong)player.CSteamID].PlayerName));
                    }
                }
                if (command.Length == 2 && command[0] == "f")
                {
                    bool IsPlayerID = ulong.TryParse(command[1], out ulong PlayerID);
                    if (IsPlayerID)
                    {
                        if (!PRMain.Instance.PRPlayer.ContainsKey(PlayerID))
                        {
                            Rocket.Core.Logging.Logger.Log(PRMain.Instance.Translate("Command_NotPlayer", command[1]));
                            return;
                        }
                        Dictionary<float, List<uint>> NewFarm = new Dictionary<float, List<uint>>
                    {
                        { 0.1f, new List<uint>() }
                    };
                        PRMain.Instance.PRPlayer[(ulong)player.CSteamID].FarminstanceID = NewFarm;
                        Rocket.Core.Logging.Logger.Log(PRMain.Instance.Translate("Command_RESPlayer", PRMain.Instance.PRPlayer[(ulong)player.CSteamID].PlayerName));
                    }
                    else
                    {
                        PlayerID = PlayerCSteamIDFromName(command[1]);
                        if (PlayerID == 0)
                        {
                            Rocket.Core.Logging.Logger.Log(PRMain.Instance.Translate("Command_NotPlayer", command[1]));
                            return;
                        }
                        if (!PRMain.Instance.PRPlayer.ContainsKey(PlayerID))
                        {
                            Rocket.Core.Logging.Logger.Log(PRMain.Instance.Translate("Command_NotPlayer", command[1]));
                            return;
                        }
                        Dictionary<float, List<uint>> NewFarm = new Dictionary<float, List<uint>>
                    {
                        { 0.1f, new List<uint>() }
                    };
                        PRMain.Instance.PRPlayer[(ulong)player.CSteamID].FarminstanceID = NewFarm;
                        Rocket.Core.Logging.Logger.Log(PRMain.Instance.Translate("Command_RESPlayer", PRMain.Instance.PRPlayer[(ulong)player.CSteamID].PlayerName));
                    }
                }
            }
        }
        public static ulong PlayerCSteamIDFromName(string PlayeerName)
        {
            foreach (var item in PRMain.Instance.PRPlayer)
            {
                if (item.Value.PlayerName.Contains(PlayeerName))
                    return item.Key;
            }
            return 0;
        }
    }
}
