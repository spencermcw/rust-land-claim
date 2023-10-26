#define DEBUG

namespace Oxide.Plugins
{
    using System.Collections.Generic;
    using UnityEngine;
    //using Oxide.Plugins.LandClaimExtensionMethods;
    using System;
    using System.Linq;
    using Oxide.Core.Configuration;
    using Oxide.Core;

    [Info("Land Claim", "SenZen", "0.1.2")]
    [Description("Allows Land Claim and Management")]
    public partial class LandClaim : RustPlugin
    {
        #region Configuration
        private const uint INITIAL_TEAM_ALLOWANCE = 2;
        private const bool REQUIRE_CONTIGUOUS = true;
        private MapGrid map = new MapGrid();
        #endregion

        #region Data
        private Hash<Parcel, ulong> parcelOwner = new Hash<Parcel, ulong>();
        private Hash<ulong, HashSet<Parcel>> teamParcels = new Hash<ulong, HashSet<Parcel>>();
        private Hash<ulong, uint> teamParcelAllowance = new Hash<ulong, uint>();
        #endregion

        private void Init()
        {
            // Initialize teamParcels
            teamParcels.Add(0, new HashSet<Parcel>());
            // Initialize Parcels
            // Width seems to consistenly be 1 larger than height?
            // Thank Facepunch's janky map UI generation...
            for (uint x = 0; x < MapGrid.GridHeight() + 1; x++)
            {
                for (uint z = 0; z < MapGrid.GridHeight(); z++)
                {
                    Parcel parcel = new Parcel(x, z);
                    parcelOwner[parcel] = 0;
                    teamParcels[0].Add(parcel);
                }
            }
            SaveData();
        }

        private void SaveData()
        {
            DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetDatafile("LandClaim");
            foreach(ulong team in teamParcels.Keys)
            {
                dataFile["teamParcels", team.ToString()] = String.Join(", ", teamParcels[team].ToArray());
            }
            dataFile.Save();
        }

        private void LoadData()
        {
        }


        #region Command Handlers
        private bool TryClaimParcel(Parcel parcel, BasePlayer player)
        {
            // Check invalid claims
            if (player.currentTeam == 0)
            {
                player.IPlayer.Reply("Must be in a Team to claim parcels.");
                return false;
            }
            if (teamParcelAllowance[player.currentTeam] < 1)
            {
                player.IPlayer.Reply("Your team has claimed the maximum number of parcels.");
                player.IPlayer.Reply("Use `/claim release` to unclaim a parcel.");
                return false;
            }
            if (parcelOwner[parcel] == player.currentTeam)
            {
                player.IPlayer.Reply("Your team already owns this parcel.");
                return false;
            }
            if (parcelOwner[parcel] > 0 && parcelOwner[parcel] != player.currentTeam)
            {
                player.IPlayer.Reply("Another team already owns this parcel.");
                return false;
            }
            if (REQUIRE_CONTIGUOUS && teamParcels[player.currentTeam].Count > 0)
            {
                bool isContiguous = false;
                foreach (Parcel parcelToCheckAgainst in teamParcels[player.currentTeam].ToList())
                {
                    float deltaX = Mathf.Abs(parcelToCheckAgainst.X - parcel.X);
                    float deltaY = Mathf.Abs(parcelToCheckAgainst.Z - parcel.Z);
                    isContiguous = (
                        deltaX < 2 &&
                        deltaY < 2 &&
                        deltaX + deltaY == 1
                    );
                    if (isContiguous)
                    {
                        break;
                    }
                }
                if (!isContiguous)
                {
                    player.IPlayer.Reply("Must be adjacent to another parcel your team owns.");
                    return false;
                }
            }
            // Claim parcel for player's team
            parcelOwner[parcel] = player.currentTeam;
            teamParcels[player.currentTeam].Add(parcel);
            teamParcelAllowance[player.currentTeam] -= 1;
            player.IPlayer.Reply("Parcel successfully claimed!");
            player.IPlayer.Reply($"Your team may claim {teamParcelAllowance[player.currentTeam]} more parcels.");
            return true;
        }

        private bool TryReleaseParcel(Parcel parcel, BasePlayer player)
        {
            if (player.currentTeam == 0)
            {
                player.IPlayer.Reply("Must be in a Team to claim parcels.");
                return false;
            }
            if (parcelOwner[parcel] == 0)
            {
                player.IPlayer.Reply("This parcel is unclaimed.");
                if (teamParcelAllowance[player.currentTeam] > 0)
                {
                    player.IPlayer.Reply("Use `/claim here` to claim this parcel.");
                }
                return false;
            }
            if (parcelOwner[parcel] > 0 && parcelOwner[parcel] != player.currentTeam)
            {
                player.IPlayer.Reply("Your team does not own this parcel.");
                return false;
            }
            // Release parcel for player's team
            parcelOwner[parcel] = 0;
            teamParcels[player.currentTeam].Remove(parcel);
            teamParcelAllowance[player.currentTeam] += 1;
            if (teamParcelAllowance[player.currentTeam] > INITIAL_TEAM_ALLOWANCE)
            {
                teamParcelAllowance[player.currentTeam] = INITIAL_TEAM_ALLOWANCE;
            }
            player.IPlayer.Reply("Parcel successfully released!");
            player.IPlayer.Reply($"Your team may claim {teamParcelAllowance[player.currentTeam]} more parcels.");
            return true;
        }

        private void ListTeamParcels(BasePlayer player)
        {
            if (player.currentTeam == 0)
            {
                player.IPlayer.Reply("Must be in a Team to claim parcels.");
                return;
            }
            if (teamParcels[player.currentTeam].Count == 0)
            {
                player.IPlayer.Reply("Your team does not own any parcels.");
                return;
            }
            // Iterate over team's parcels
            player.IPlayer.Reply("Your team owns the following parcels:");
            List<string> parcels = new List<string>();
            foreach (Parcel parcel in teamParcels[player.currentTeam])
            {
                parcels.Add(map.IDForParcel(parcel));
            }
            player.IPlayer.Reply(String.Join(", ", parcels.ToArray()));
        }

        private void DrawParcelCube(BasePlayer player, Parcel parcel)
        {
            Vector3 origin = map.ParcelToWorldSpace(parcel);
            float time = 10f;
            float height = 100f;

            Vector3 point1 = origin;
            Vector3 point2 = new Vector3(origin.x + MapGrid.GRID_CELL_SIZE, origin.y, origin.z);
            Vector3 point3 = new Vector3(origin.x + MapGrid.GRID_CELL_SIZE, origin.y, origin.z + MapGrid.GRID_CELL_SIZE);
            Vector3 point4 = new Vector3(origin.x, origin.y, origin.z + MapGrid.GRID_CELL_SIZE);

            Vector3 point5 = new Vector3(origin.x, origin.y + height, origin.z);
            Vector3 point6 = new Vector3(origin.x + MapGrid.GRID_CELL_SIZE, origin.y + height, origin.z);
            Vector3 point7 = new Vector3(origin.x + MapGrid.GRID_CELL_SIZE, origin.y + height, origin.z + MapGrid.GRID_CELL_SIZE);
            Vector3 point8 = new Vector3(origin.x, origin.y + height, origin.z + MapGrid.GRID_CELL_SIZE);

            player.SendConsoleCommand("ddraw.line", time, Color.green, point1, point2);
            player.SendConsoleCommand("ddraw.line", time, Color.green, point2, point3);
            player.SendConsoleCommand("ddraw.line", time, Color.green, point3, point4);
            player.SendConsoleCommand("ddraw.line", time, Color.green, point4, point1);

            player.SendConsoleCommand("ddraw.line", time, Color.green, point1, point5);
            player.SendConsoleCommand("ddraw.line", time, Color.green, point2, point6);
            player.SendConsoleCommand("ddraw.line", time, Color.green, point3, point7);
            player.SendConsoleCommand("ddraw.line", time, Color.green, point4, point8);

            player.SendConsoleCommand("ddraw.line", time, Color.green, point5, point6);
            player.SendConsoleCommand("ddraw.line", time, Color.green, point6, point7);
            player.SendConsoleCommand("ddraw.line", time, Color.green, point7, point8);
            player.SendConsoleCommand("ddraw.line", time, Color.green, point8, point5);
        }

        private void DrawTeamParcels(BasePlayer player)
        {
            foreach (Parcel parcel in teamParcels[player.currentTeam].ToList())
            {
                DrawParcelCube(player, parcel);
            }
        }
        #endregion

        #region Hooks
        object OnPlayerCommand(BasePlayer player, string command, string[] args)
        {
            object returnValue = null;

            if (player == null) return null;
            if (command != "claim") return null;

            Parcel parcel = map.ParcelForLocation(player.transform.position);
            string action = args.Length == 0 ? "here" : args[0];
            switch (action)
            {
                case "here":
                    Puts("Claiming Parcel");
                    TryClaimParcel(parcel, player);
                    returnValue = 1;
                    break;

                case "release":
                    Puts("Releasing Parcel");
                    TryReleaseParcel(parcel, player);
                    returnValue = 1;
                    break;

                case "list":
                    Puts("Listing Team Claims");
                    ListTeamParcels(player);
                    returnValue = 1;
                    break;

                case "show":
                    Puts("Showing Team Claims");
                    DrawTeamParcels(player);
                    returnValue = 1;
                    break;

                default:
                    returnValue = null;
                    break;
            }

            return returnValue;
        }

        object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            BasePlayer player = planner.GetOwnerPlayer();
            Parcel parcel = map.ParcelForLocation(player.transform.position);
            // Check if part of a team
            if (player.currentTeam == 0)
            {
                player.IPlayer.Reply("Must be in a team to claim parcels and build.");
                return false;
            }
            // Check if team owns parcel
            if (parcelOwner[parcel] != player.currentTeam)
            {
                player.IPlayer.Reply("Your team does not own this parcel.");
                return false;
            }
            return null;
        }

        object OnTeamCreated(BasePlayer player)
        {
            teamParcels[player.currentTeam] = new HashSet<Parcel>();
            teamParcelAllowance[player.currentTeam] = INITIAL_TEAM_ALLOWANCE;
            player.IPlayer.Reply($"Your new team may claim up to {teamParcelAllowance[player.currentTeam]} parcels");
            return null;
        }

        void OnTeamDisbanded(RelationshipManager.PlayerTeam team)
        {
            teamParcelAllowance[team.teamID] = 0;
            if (teamParcels[team.teamID] != null)
            {
                teamParcels[team.teamID].Clear();
            }
        }
        #endregion
    }
}


namespace Oxide.Plugins
{
    using System;
    using System.Text.RegularExpressions;
    using UnityEngine;

    public struct Parcel
    {
        public Parcel(uint x, uint z)
        {
            X = x;
            Z = z;
            IDAlpha = MapGrid.NumToAlpha((int)X);
            IDNum = MapGrid.GridHeight() - Z - 1;
        }
        public uint X { get; private set; }
        public uint Z { get; private set; }
        public string IDAlpha { get; private set; }
        public uint IDNum { get; private set; }
        public string ID()
        {
            return $"{IDAlpha}{IDNum}";
        }
    }

    public class MapGrid
    {
        public static readonly float GRID_CELL_SIZE = 146.3f;
        public float Size { get; private set; }
        public float Origin { get; private set; }
        public float Offset { get; private set; }

        public static uint GridHeight()
        {
            return (uint)Mathf.FloorToInt(TerrainMeta.Size.z / GRID_CELL_SIZE);
        }

        public MapGrid()
        {
            Size = TerrainMeta.Size.x;
            Offset = TerrainMeta.Size.x / 2;
        }

        public Parcel ParcelForLocation(Vector3 location)
        {
            Vector3 normalizationOffset = new Vector3(Offset, 0, Offset);
            Vector3 normalizedCoordinates = location + normalizationOffset;
            Parcel cell = new Parcel(
                (uint)Mathf.Floor(normalizedCoordinates.x / GRID_CELL_SIZE),
                (uint)Mathf.Floor(normalizedCoordinates.z / GRID_CELL_SIZE)
            );
            return cell;
        }

        public Vector3 ParcelToWorldSpace(Parcel parcel)
        {
            Vector3 worldSpace = new Vector3(
                (parcel.X * GRID_CELL_SIZE) - Offset,
                0,
                (parcel.Z * GRID_CELL_SIZE) - Offset
            );
            return worldSpace;
        }

        public Parcel ParcelForID(string ID)
        {
            Regex rx = new Regex(@"([A-Z]+)(\d+)");
            MatchCollection matches = rx.Matches(ID);
            string alpha = matches[0].Groups[1].Value;
            int num = Convert.ToInt32(matches[0].Groups[2].Value);
            Parcel parcel = new Parcel((uint)AlphaToNum(alpha), (uint)(GridHeight() - num - 1));
            return parcel;
        }

        public string IDForParcel(Parcel parcel)
        {
            string parcelAlpha = NumToAlpha((int)parcel.X);
            uint parcelNum = GridHeight() - (uint)parcel.Z - 1;
            string parcelAlphaNum = $"{parcelAlpha}{parcelNum}";
            return parcelAlphaNum;
        }

        public static string NumToAlpha(int num)
        {
            const string ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string alpha = "";
            int _num = num;
            while (_num >= 0)
            {
                int index = _num % 26;
                char character = ALPHABET[index];
                alpha = character + alpha;
                if (_num < 26) break;
                _num = (_num - index) / 26 - 1;
            }
            return alpha;
        }

        public static int AlphaToNum(string alpha)
        {
            const string ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            int num = 0;
            for (int i = 0; i < alpha.Length; i++)
            {
                int idx = ALPHABET.IndexOf(alpha[i]);
                int exp = alpha.Length - 1 - i;
                num += idx + (exp * ALPHABET.Length);
            }
            return num;
        }
    }
}

