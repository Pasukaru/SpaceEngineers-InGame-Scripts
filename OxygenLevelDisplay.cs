using System;
using System.Collections.Generic;

using Sandbox.ModAPI.Ingame;

public class OxygenLevelDisplay
{
    IMyGridTerminalSystem GridTerminalSystem = null;
    #region CodeEditor
    //************************ Oxygen Level Display ************************\\        
    // Build a Timer Block and configure it so that the first action points to this Programmable Block -> "Run".   
    // Enter the names of the blocks in the variables below.   
    // Make sure all blocks belong to the same user, hit the Run button and that's it!  

    //***** Configuration *****\\   

    //Name of the LCD/Text Panel that will display the text  
    const string LCD_PANEL = "LCD (Oxygen Status)";

    //Timerblock  
    const string TIMER_BLOCK = "Timer Block (Oxygen Status)";

    //Update every second (1000ms).  
    //Higher values result in less frequent updates to the panel but will also consume less CPU.  
    const long INTERVAL = 1000;

    //Message format 
    const string FORMAT = "Oxygen Level\nAvg: {0:0.00000}%\nTanks: {1:00}\nTime Left: {2:00}:{3:00}:{4:00}";

    //***** DO NOT EDIT BELOW THIS LINE *****//  

    long last = 0;

    void nextTick()
    {
        var tb = GridTerminalSystem.GetBlockWithName(TIMER_BLOCK);
        if (tb != null && (tb is IMyTimerBlock))
        {
            tb.GetActionWithName("TriggerNow").Apply(tb);
        }
    }

    void Main()
    {
        var now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        if (now - last < INTERVAL)
        {
            nextTick();
            return;
        }
        last = now;

        var panel = GridTerminalSystem.GetBlockWithName(LCD_PANEL) as IMyTerminalBlock;

        if (panel == null || !(panel is IMyTextPanel))
        {
            nextTick();
            return;
        }

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

        (panel as IMyTextPanel).WritePublicText(String.Format(FORMAT, oxygen, tanks.Count, Math.Floor(timeLeft.TotalHours), timeLeft.Minutes, timeLeft.Seconds));
        (panel as IMyTextPanel).ShowPublicTextOnScreen();
        nextTick();
    }
    #endregion
}
