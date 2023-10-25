
namespace Oxide.Plugins
{

    using System.Collections.Generic;
    using UnityEngine;
    //using Oxide.Plugins.LandClaimExtensionMethods;
    using System;

    [Info("Land Claim", "SenZen", "0.1.1")]
    [Description("Allows Land Claim and Management")]
    public partial class LandClaim : RustPlugin
    {
        #region Configuration
        // TODO: Convert to settings
        private const uint MapSize = 4500;
        private const uint MapWidth = 31;
        private const uint MapHeight = 30;
        private const uint InitialTeamAllowance = 5;

        //MapSize = Math.Floor(TerrainMeta.Size.x / CellSize) * CellSize;
        //MapWidth = Mathf.Floor(TerrainMeta.Size.x / CellSize) * CellSize;
        //MapHeight = Mathf.Floor(TerrainMeta.Size.z / CellSize) * CellSize;
        //NumberOfRows = (int)Math.Floor(MapHeight / (float)CellSize);
        //NumberOfColumns = (int)Math.Floor(MapWidth / (float)CellSize);

        private const float MapOrigin = -(MapSize / 2);
        private const float CellLength = 146.3f;
        #endregion


        #region Data
        public struct Parcel
        {
            public Parcel(uint id, uint x, uint y, ulong team)
            {
                ID = id;
                X = x;
                Y = y;
                Team = team;
            }

            public uint ID;
            public uint X;
            public uint Y;
            public ulong Team;
            // public uint duration?
        }

        private Hash<ulong, uint> teamParcelAllowance = new Hash<ulong, uint>();
        private Hash<uint, Parcel> parcelById = new Hash<uint, Parcel>();
        private Hash<ulong, HashSet<uint>> teamParcels = new Hash<ulong, HashSet<uint>>();
        #endregion


        private void Init()
        {
            Puts("LandClaim plugin initialized.");
            // Initialize teamParcels
            teamParcels.Add(0, new HashSet<uint>());
            // Initialize Parcels
            Puts("Creating Parcels...");
            for (uint w = 0; w < MapWidth; w++)
            {
                for (uint h = 0; h < MapHeight; h++)
                {
                    uint id = w + h * MapWidth;
                    //Puts($"Creating Parcel: {id} @ ({w}, {h})");
                    Parcel parcel = new Parcel(id, w, h, 0);
                    parcelById.Add(id, parcel);
                    teamParcels[0].Add(id);
                }
            }

            //Puts($"Created {MapWidth * MapWidth} Parcels.");
            //Puts($"CellSize: {CellLength}");
            //for (uint i = 0; i <= MapWidth; i++)
            //{
            //    Puts($"teleportpos ({MapOrigin + (i * CellLength)},100,{MapOrigin + (i * CellLength)})");
            //}
        }

        private void DrawWorldGrid(BasePlayer player)
        {
            Puts($"Drawing World Grid");
            float time = 15f;

            Vector3 origin = new Vector3(MapOrigin, 100f, MapOrigin);
            // TODO
        }

        private void DrawPrarcelCube(BasePlayer player, Vector3 origin)
        {
            Puts($"Drawing Parcel Cube @ {origin}");
            float time = 15f;

            Vector3 point1 = origin;
            Vector3 point2 = new Vector3(origin.x + CellLength, origin.y, origin.z);
            Vector3 point3 = new Vector3(origin.x + CellLength, origin.y, origin.z + CellLength);
            Vector3 point4 = new Vector3(origin.x, origin.y, origin.z + CellLength);

            player.SendConsoleCommand("ddraw.line", time, Color.blue, point1, point2);
            player.SendConsoleCommand("ddraw.line", time, Color.blue, point2, point3);
            player.SendConsoleCommand("ddraw.line", time, Color.blue, point3, point4);
            player.SendConsoleCommand("ddraw.line", time, Color.blue, point4, point1);

            //        Vector3 point1 = RotatePointAroundPivot(new Vector3(center.x + size.x, center.y + size.y, center.z + size.z), center, rotation);
            //        Vector3 point2 = RotatePointAroundPivot(new Vector3(center.x + size.x, center.y - size.y, center.z + size.z), center, rotation);
            //        Vector3 point3 = RotatePointAroundPivot(new Vector3(center.x + size.x, center.y + size.y, center.z - size.z), center, rotation);
            //        Vector3 point4 = RotatePointAroundPivot(new Vector3(center.x + size.x, center.y - size.y, center.z - size.z), center, rotation);
            //        Vector3 point5 = RotatePointAroundPivot(new Vector3(center.x - size.x, center.y + size.y, center.z + size.z), center, rotation);
            //        Vector3 point6 = RotatePointAroundPivot(new Vector3(center.x - size.x, center.y - size.y, center.z + size.z), center, rotation);
            //        Vector3 point7 = RotatePointAroundPivot(new Vector3(center.x - size.x, center.y + size.y, center.z - size.z), center, rotation);
            //        Vector3 point8 = RotatePointAroundPivot(new Vector3(center.x - size.x, center.y - size.y, center.z - size.z), center, rotation);

            //        player.SendConsoleCommand("ddraw.line", time, Color.blue, point1, point2);
            //        player.SendConsoleCommand("ddraw.line", time, Color.blue, point1, point3);
            //        player.SendConsoleCommand("ddraw.line", time, Color.blue, point1, point5);
            //        player.SendConsoleCommand("ddraw.line", time, Color.blue, point4, point2);
            //        player.SendConsoleCommand("ddraw.line", time, Color.blue, point4, point3);
            //        player.SendConsoleCommand("ddraw.line", time, Color.blue, point4, point8);

            //        player.SendConsoleCommand("ddraw.line", time, Color.blue, point5, point6);
            //        player.SendConsoleCommand("ddraw.line", time, Color.blue, point5, point7);
            //        player.SendConsoleCommand("ddraw.line", time, Color.blue, point6, point2);
            //        player.SendConsoleCommand("ddraw.line", time, Color.blue, point8, point6);
            //        player.SendConsoleCommand("ddraw.line", time, Color.blue, point8, point7);
            //        player.SendConsoleCommand("ddraw.line", time, Color.blue, point7, point3);
        }




        /*
         * TODOs
         * Team disband - no members left in team to own parcels
         * Only teams can claim parcels
         * Parcel Limits
         * Contiguous Parcels
        */

        #region Helper Functions
        private Parcel ParcelOfPlayerLocation(BasePlayer player)
        {
            Vector3 playerPosition = player.transform.position;
            Puts(playerPosition.ToString());
            return parcelById[0];
        }
        #endregion



        #region Hooks
        object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            Puts("CanBuild works!");
            // Check if part of a team
            // Check if team owns parcel
            return null;
        }
        #endregion

        #region Command Handling
        object OnPlayerCommand(BasePlayer player, string command, string[] args)
        {
            object returnValue = null;
            Puts(command);

            if (player == null) return null;
            if (command != "claim") return null;
            if (player.Team == null)
            {
                //Puts("No team");
                player.IPlayer.Reply("Must be in a Team to claim parcels.");
                return 1;
            }

            string action = args.Length == 0 ? "here" : args[0];
            Puts(args.Length.ToString(), action);
            switch (action)
            {
                case "here":
                    ParcelOfPlayerLocation(player);
                    Puts("Claiming Parcel");
                    break;

                case "release":
                    Puts("Releasing Parcel");
                    break;

                case "drawworld":
                    Puts("Drawing World");
                    DrawWorldGrid(player);
                    break;

                case "draw":
                    Puts("Drawing");
                    DrawPrarcelCube(player, player.transform.position);
                    break;

                default:
                    returnValue = null;
                    break;
            }

            return returnValue;
        }

        object OnTeamCreated(BasePlayer player)
        {
            Puts("OnTeamCreated!");
            teamParcelAllowance[player.Team.teamID] = InitialTeamAllowance;
            return null;
        }

        void OnTeamDisbanded(RelationshipManager.PlayerTeam team)
        {
            Puts("OnTeamDisbanded!");
            teamParcelAllowance[team.teamID] = 0;
            // TODO: Remove all parcels
        }
        #endregion
    }
}

