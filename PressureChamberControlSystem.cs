using System;
using System.Collections.Generic;

using Sandbox.ModAPI.Ingame;

public class Class1
{
    IMyGridTerminalSystem GridTerminalSystem = null;
    #region CodeEditor
    //************************ Pressure Chamber Control System ************************\\      
    // Build a small room with two doors and one vent.  
    // Build a Timer Block and configure it so that the first action points to this Programmable Block -> "Run". 
    // Enter the names of the blocks in the variables below. 
    // For an easy use, you should build 3 Button Panels. One next to each door (outside) plus one in the center 
    // of the Pressure Chamber. 

    //***** Configuration *****\\

    //Name of the Inner Door 
    const string DOOR_INSIDE = "Door (Pressure Chamber Inside)";

    //Name of the Outer Door 
    const string DOOR_OUTSIDE = "Door (Pressure Chamber Outside)";

    //Name of the Vent 
    const string VENT = "Air Vent (Pressure Chamber)";

    //Name of the Timer block
    const string TIMER_BLOCK = "Timer Block (Pressure Chamber)";

    //Name of the LCD Panel the PB will print its messages onto. (OPTIONAL)
    const string LOG_PANEL = "LCD Log (Pressure Chamber)";

    //Set this to false, if you don't want the PB to power off doors.
    //This prevents manual open/close and accidental loss of air.
    const bool DISABLE_DOORS = true;

    //Don't wait longer than 3 seconds (180 ticks) for depressurization.
    //Low values will result in a loss of air if the vent is too slow or your Oxygen Tanks are full.
    const int MAX_WAIT_DEPRESSURIZING = 180;

    //Don't wait longer than 3 seconds (180 ticks) for pressurization.
    //Low values probably won't harm you, as you will receive oxygen 
    //from the main room if the door opens.
    //This is mostly useful if your tanks are empty.
    //Set to 0 if you want the inner door to open immediately.
    const int MAX_WAIT_PRESSURIZING = 180;

    //The number of frames until the door is fully closed.
    //You probably don't need to change this.
    const int DOOR_ANIMATION_DURATION = 70;

    //***** DO NOT MODIFY BELOW THIS LINE *****\\

    const string OPEN_DOOR = "Open_On";
    const string CLOSE_DOOR = "Open_Off";
    const string ENABLE = "OnOff_On";
    const string DISABLE = "OnOff_Off";
    const string DE_PRESSURIZE = "Depressurize_On";
    const string PRESSURIZE = "Depressurize_Off";

    public Func<bool> wait = null;
    public List<Func<Func<bool>>> actions = new List<Func<Func<bool>>>();
    public int currentAction = 0;

    public IMyTimerBlock timerBlock = null;
    public IMyTextPanel logPanel = null;

    public void nextTick()
    {
        var tb = GridTerminalSystem.GetBlockWithName(TIMER_BLOCK) as IMyTimerBlock;
        timerBlock.ApplyAction("TriggerNow");
    }

    public void c_log(string message)
    {
        if (logPanel == null) { return; }
        if (message == null)
        {
            logPanel.WritePublicText("");
        }
        else
        {
            logPanel.WritePublicText(message + "\n", true);
        }
        logPanel.ShowPublicTextOnScreen();
    }

    public Func<bool> waitForTicks(long ticks)
    {
        long count = 0;
        return () =>
        {
            return count++ >= ticks;
        };
    }

    public Func<bool> waitForPressure(IMyAirVent vent, bool pressurizing)
    {
        Func<bool> maxWait = waitForTicks(pressurizing ? MAX_WAIT_PRESSURIZING : MAX_WAIT_DEPRESSURIZING);
        return () =>
        {
            double ox = vent.GetOxygenLevel();
            if (maxWait()) {
                c_log((pressurizing ? "" : "de")+"pressurization_wait_timout ("+(ox*100)+"% oxygen lost)");
                return true; 
            }
            if (pressurizing)
            {
                if (ox >= 0.95)
                {
                    return true;
                }
            }
            else
            {
                if (ox <= 0.0001)
                {
                    return true;
                }
            }
            return false;
        };
    }

    public class BlockAction
    {
        protected readonly string action;
        protected readonly Func<bool> wait;
        protected readonly IMyTerminalBlock[] blocks;
        public BlockAction(string action, Func<bool> wait, params IMyTerminalBlock[] blocks)
        {
            this.action = action;
            this.wait = wait;
            this.blocks = blocks;
        }
        public Func<bool> execute()
        {
            foreach (IMyTerminalBlock block in blocks)
            {
                block.ApplyAction(action);
            }
            return wait;
        }
    }

    public Func<Func<bool>> logWrapper(string message, Func<Func<bool>> action)
    {
        return () =>
        {
            c_log("Executing: " + message);
            return action();
        };
    }

    public void addDoorAction(string msg, Func<Func<bool>> action)
    {
        if (DISABLE_DOORS)
        {
            actions.Add(logWrapper(msg, action));
        }
    }

    void Main()
    {
        if (wait != null)
        {
            if (!wait())
            {
                nextTick();
                return;
            }
            wait = null;
        }

        if (actions.Count == 0)
        {
            var logPanelBlock = GridTerminalSystem.GetBlockWithName(LOG_PANEL);
            logPanel = (logPanelBlock != null && logPanelBlock is IMyTextPanel)
                ? (IMyTextPanel)logPanelBlock
                : null;

            timerBlock = GridTerminalSystem.GetBlockWithName(TIMER_BLOCK) as IMyTimerBlock;
            var door_outside = GridTerminalSystem.GetBlockWithName(DOOR_OUTSIDE) as IMyDoor;
            var door_inside = GridTerminalSystem.GetBlockWithName(DOOR_INSIDE) as IMyDoor;
            var vent = GridTerminalSystem.GetBlockWithName(VENT) as IMyAirVent;
            c_log(null);

            addDoorAction("enable_doors", new BlockAction(ENABLE, waitForTicks(0), door_outside, door_inside).execute);
            actions.Add(logWrapper("close_doors", new BlockAction(CLOSE_DOOR, waitForTicks(DOOR_ANIMATION_DURATION), door_outside, door_inside).execute));
            addDoorAction("disable_doors", new BlockAction(DISABLE, null, door_outside, door_inside).execute);
            actions.Add(logWrapper("enable_vent", new BlockAction(ENABLE, waitForTicks(0), vent).execute));
            if (door_outside.Open)
            {
                actions.Add(logWrapper("pressurize", new BlockAction(PRESSURIZE, waitForPressure(vent, true), vent).execute));
                addDoorAction("enable_inner_door", new BlockAction(ENABLE, waitForTicks(0), door_inside).execute);
                actions.Add(logWrapper("disable_vent", new BlockAction(DISABLE, waitForTicks(0), vent).execute));
                actions.Add(logWrapper("open_inner_door", new BlockAction(OPEN_DOOR, waitForTicks(DOOR_ANIMATION_DURATION), door_inside).execute));
                addDoorAction("disable_inner_door", new BlockAction(DISABLE, null, door_inside).execute);
            }
            else
            {
                actions.Add(logWrapper("depressurize", new BlockAction(DE_PRESSURIZE, waitForPressure(vent, false), vent).execute));
                addDoorAction("enable_outer_door", new BlockAction(ENABLE, waitForTicks(0), door_outside).execute);
                actions.Add(logWrapper("disable_vent", new BlockAction(DISABLE, waitForTicks(0), vent).execute));
                actions.Add(logWrapper("open_outer_door", new BlockAction(OPEN_DOOR, waitForTicks(DOOR_ANIMATION_DURATION), door_outside).execute));
                addDoorAction("disable_outer_door", new BlockAction(DISABLE, null, door_outside).execute);
            }
        }

        while (wait == null && currentAction < actions.Count)
        {
            wait = actions[currentAction++]();
        }

        if (currentAction < actions.Count)
        {
            nextTick();
        }
        else
        {
            currentAction = 0;
            actions.Clear();
        }
    }
    #endregion
}
