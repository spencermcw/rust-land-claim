#define DEBUG

namespace Oxide.Plugins
{
    using System.Collections.Generic;
    using UnityEngine;
    //using Oxide.Plugins.LandClaimExtensionMethods;
    using System;
    using System.Linq;

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
        private Hash<Vector2, ulong> parcelOwner = new Hash<Vector2, ulong>();
        private Hash<ulong, HashSet<Vector2>> teamParcels = new Hash<ulong, HashSet<Vector2>>();
        private Hash<ulong, uint> teamParcelAllowance = new Hash<ulong, uint>();
        #endregion

        private void Init()
        {
            // Initialize teamParcels
            teamParcels.Add(0, new HashSet<Vector2>());
            // Initialize Parcels
            for (uint x = 0; x < map.Width; x++)
            {
                for (uint z = 0; z < map.Height; z++)
                {
                    Vector2 parcel = new Vector2(x, z);
                    parcelOwner[parcel] = 0;
                    teamParcels[0].Add(parcel);
                }
            }
        }


        #region Command Handlers
        private bool TryClaimParcel(Vector2 parcel, BasePlayer player)
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
                foreach (Vector2 parcelToCheckAgainst in teamParcels[player.currentTeam].ToList())
                {
                    float deltaX = Mathf.Abs(parcelToCheckAgainst.x - parcel.x);
                    float deltaY = Mathf.Abs(parcelToCheckAgainst.y - parcel.y);
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

        private bool TryReleaseParcel(Vector2 parcel, BasePlayer player)
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
            foreach (Vector2 parcel in teamParcels[player.currentTeam])
            {
                parcels.Add(map.ParcelID(parcel));
            }
            player.IPlayer.Reply(String.Join(", ", parcels.ToArray()));
        }

        private void DrawParcelCube(BasePlayer player, Vector2 parcel)
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
            foreach (Vector2 parcel in teamParcels[player.currentTeam].ToList())
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

            Vector2 parcel = map.ParcelForCoordinates(player.transform.position);
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
            Vector2 parcel = map.ParcelForCoordinates(player.transform.position);
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
            teamParcels[player.currentTeam] = new HashSet<Vector2>();
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

    public class MapGrid
    {
        // Rust doesn't consistently calculate width/height for maps so
        // we have to manually specify the number of rows...
        private const uint GRID_ROWS = 30;
        public static readonly float GRID_CELL_SIZE = 146.3f;
        public float Size { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }
        public float Origin { get; private set; }
        public float Offset { get; private set; }


        public MapGrid()
        {
            Size = TerrainMeta.Size.x;
            Offset = TerrainMeta.Size.x / 2;
            Width = Mathf.Ceil(TerrainMeta.Size.x / GRID_CELL_SIZE);
            Height = Mathf.Ceil(TerrainMeta.Size.z / GRID_CELL_SIZE);
        }

        public Vector2 ParcelForCoordinates(Vector3 coordinates)
        {
            Vector3 normalizationOffset = new Vector3(Offset, 0, Offset);
            Vector3 normalizedCoordinates = coordinates + normalizationOffset;
            Vector2 cell = new Vector2(
                Mathf.Floor(normalizedCoordinates.x / GRID_CELL_SIZE),
                Mathf.Floor(normalizedCoordinates.z / GRID_CELL_SIZE)
            );
            return cell;
        }

        public Vector3 ParcelToWorldSpace(Vector2 parcel)
        {
            Vector3 worldSpace = new Vector3(
                (parcel.x * GRID_CELL_SIZE) - Offset,
                0,
                (parcel.y * GRID_CELL_SIZE) - Offset
            );
            Console.WriteLine($"Parcel: {parcel}\nWorldSpace: {worldSpace}");
            return worldSpace;
        }

        public string ParcelID(Vector2 parcel)
        {
            // Calculate Alpha
            const string ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string parcelAlpha = "";
            int x = (int)parcel.x;

             while (x >= 0)
            {
                int index = x % 26;
                char character = ALPHABET[index];
                parcelAlpha = character + parcelAlpha;
                if (x < 26) break;
                x = (x - index) / 26 - 1;
            }

            uint parcelNum = GRID_ROWS - (uint)parcel.y - 1;
            string parcelAlphaNum = $"{parcelAlpha}{parcelNum}";
            return parcelAlphaNum;
        }
    }
}


//namespace Oxide.Plugins.LandClaimExtensionMethods
//{
//    public static class ExtensionMethods
//    {
//    }
//}

