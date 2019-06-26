﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Harmony;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace TwentyFourHours.Framework
{
    internal class DayTimeMoneyBoxPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Algorithm for patching this method:
            // 1 - Find the unique Ldstr instruction with ":"
            // 2 - Once the ":" is found, save the next Stloc_S instruction. It's the instruction to save the variable that we want to modify
            // 3 - Still right after the ":", find the Ldsfld instruction that as "dialogueFont" in operand, we will inject our code right after this instruction

            var instructionsList = instructions.ToList();

            int foundIndex = -1;
            bool foundColon = false;
            bool foundInstruction = false;
            CodeInstruction instructionMemory = null;

            for (var i = 0; i < instructionsList.Count; i++)
            {
                var instruction = instructionsList[i];

                if (instruction.opcode == OpCodes.Ldstr && instruction.operand as string == ":")
                {
                    foundColon = true;
                }

                if (foundColon && instruction.opcode == OpCodes.Stloc_S)
                {
                    instructionMemory = instruction;
                    foundColon = false;
                    foundInstruction = true;
                }

                if (foundInstruction && instruction.opcode == OpCodes.Ldsfld && instruction.operand != null && instruction.operand.ToString() == $"{typeof(SpriteFont).FullName} dialogueFont")
                {
                    foundIndex = i;
                    foundInstruction = false;
                }
            }

            // Injection details:
            // 1 - Load Game1.timeOfDay on the execution stack
            // 2 - Call our internal method to compute the time
            // 3 - Save the variable with the instruction we have found earlier
            CodeInstruction[] insertions = {
                new CodeInstruction(OpCodes.Ldsfld, typeof(Game1).GetField(nameof(Game1.timeOfDay))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DayTimeMoneyBoxPatch), nameof(DayTimeMoneyBoxPatch.ComputeTime))),
                instructionMemory
            };

            instructionsList.InsertRange(foundIndex + 1, insertions);
            return instructionsList.AsEnumerable();
        }

        private static string ComputeTime(int time)
        {
            return ModEntry.ConvertTime(time);
        }
    }
}
