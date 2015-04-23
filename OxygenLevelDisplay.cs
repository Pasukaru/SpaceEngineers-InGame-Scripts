using System;
using System.Collections.Generic;

using Sandbox.ModAPI.Ingame;

public class OxygenLevelDisplay
{
    IMyGridTerminalSystem GridTerminalSystem = null;
    #region CodeEditor
    //************************ Oxygen Level Display ************************\\
    // Build an LCD Panel (I recommend a wide one with font size 4.5).
    // Build a Timer Block and configure it:
    // Action 1: points to this Programmable Block -> "Run".
    // Action 2: points to the Timer Block (itself) -> "Start".
    // Set the delay to 1 second for faster updates. Higher values will consume
    // less CPU though.
    // Make sure all blocks belong to the same user.
    // Configure this script (see below).
    // Start the timer.

    //***** Configuration *****\\   

    //Name of the LCD/Text Panel that will display the text  
    const string LCD_PANEL = "LCD (Oxygen Status)";

    //Message format 
    const string FORMAT = "Oxygen Level\nAvg: {0:0.00000}%\nTanks: {1:00}\nTime Left: {2:00}:{3:00}:{4:00}";

    //Set this to false if you don't know why something is not working properly.
    //However, you will have to recompile (Remember & Exit) the script if an error occurs.
    const bool IGNORE_ERRORS = true;

    //***** DO NOT EDIT BELOW THIS LINE *****//  

    void Main()
    {

        var panel_block = GridTerminalSystem.GetBlockWithName(LCD_PANEL) as IMyTerminalBlock;

        if (panel_block == null || !(panel_block is IMyTextPanel))
        {
            if (!IGNORE_ERRORS)
            {
                throw new Exception("Failed to find panel with name: " + LCD_PANEL);
            }
            return;
        }

        IMyTextPanel panel = (IMyTextPanel)panel_block;

        double oxygen = 0;
        var tanks = new List<IMyTerminalBlock>();
        GridTerminalSystem.GetBlocksOfType<IMyOxygenTank>(tanks);
        if (tanks.Count > 0)
        {
            for (int i = 0; i < tanks.Count; i++)
            {
                oxygen += (tanks[i] as IMyOxygenTank).GetOxygenLevel();
            }
            oxygen /= tanks.Count / 100f;
        }

        var timeLeft = TimeSpan.FromSeconds(Math.Min(oxygen * tanks.Count / 0.0001, 359999));
        panel.WritePublicText(String.Format(FORMAT, oxygen, tanks.Count, Math.Floor(timeLeft.TotalHours), timeLeft.Minutes, timeLeft.Seconds));
        panel.ShowPublicTextOnScreen();
    }
    #endregion
}
