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
     
    //Set this to false if you want to ignore docked ships 
    const bool IGNORE_OTHER_GRIDS = true; 

    // Number of astronauts is used to calculate the remaining time. i.e: 2 would have have the time left.
    const int ASTRONAUTS = 1;
     
    //Message format  
    const string FORMAT = "Oxygen Level: {0:0.00}%\nCapacity: {5:00000}/{6:00000}\nTanks: {1}\nTime Left: {2:00}:{3:00}:{4:00}"; 
     
    //Set this to false if you don't know why something is not working properly. 
    //However, you will have to recompile (Remember & Exit) the script if an error occurs. 
    const bool IGNORE_ERRORS = true; 
     
    //***** DO NOT EDIT BELOW THIS LINE *****//   

    const double O2_CONSUMPTION = 0.063;
     
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
     
        var tanks = new List<IMyTerminalBlock>(); 
        GridTerminalSystem.GetBlocksOfType<IMyGasTank>(tanks, x=>x.DetailedInfo.Split(' ')[1] == "Oxygen"); 
        if (IGNORE_OTHER_GRIDS) 
        { 
            tanks = tanks.Where(tank => tank.CubeGrid == Me.CubeGrid).ToList(); 
        } 
     
        double oxygen = 0f;
        var capacity = 0f;
        if (tanks.Count > 0) 
        { 
            for (int i = 0; i < tanks.Count; i++) 
            { 
                var tank = (tanks[i] as IMyGasTank);
                capacity += tank.Capacity;
                oxygen += tank.Capacity * tank.FilledRatio;
            }
        }
        var percent = oxygen / capacity * 100;

        var timeLeft = TimeSpan.FromSeconds(Math.Min(oxygen / O2_CONSUMPTION / ASTRONAUTS, 359999)); 
        panel.WriteText(String.Format(FORMAT, percent, tanks.Count, 
          Math.Floor(timeLeft.TotalHours), timeLeft.Minutes, timeLeft.Seconds, 
          oxygen, capacity)
        );
    }
    #endregion
}
