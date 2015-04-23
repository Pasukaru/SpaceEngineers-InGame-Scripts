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
    const string VENT = "Vent (Pressure Chamber)";

    //Name of the Timer block
    const string TIMER_BLOCK = "Timer Block (Pressure Chamber)";

    //Set this to false, if you don't want the PB to power off doors.
    //This prevents manual open/close and accidental loss of air.
    const bool DISABLE_DOORS = true;

    //Dont wait longer than 3 seconds for depressurization.
    //Low values will result in a loss of air if the vent is too slow or your Oxygen Tanks are full.
    const int MAX_WAIT_DEPRESSURIZING = 3;

    //Don't wait longer than 3 seconds for pressurization.
    //Low values probably won't harm you, as you will receive oxygen 
    //from the main room if the door opens.
    //This is mostly useful if your tanks are empty.
    //Set to 0 if you want the inner door to open immediately.
    const int MAX_WAIT_PRESSURIZING = 3;

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

    const string RUN = "Run";

    string action = null;
    string state = null;
    int timer = 0;

    void nextTick()
    {
        timer++;
        var tb = GridTerminalSystem.GetBlockWithName(TIMER_BLOCK) as IMyTimerBlock;
        tb.GetActionWithName("TriggerNow").Apply(tb);
    }

    void enableDoor(IMyDoor door)
    {
        door.GetActionWithName(ENABLE).Apply(door);
    }

    void disableDoor(IMyDoor door)
    {
        door.GetActionWithName(DISABLE).Apply(door);
    }

    void openDoor(IMyDoor door)
    {
        enableDoor(door);
        door.GetActionWithName(OPEN_DOOR).Apply(door);
    }

    void closeDoor(IMyDoor door)
    {
        enableDoor(door);
        door.GetActionWithName(CLOSE_DOOR).Apply(door);
    }

    void Main()
    {
        var door_outside = GridTerminalSystem.GetBlockWithName(DOOR_OUTSIDE) as IMyDoor;
        var door_inside = GridTerminalSystem.GetBlockWithName(DOOR_INSIDE) as IMyDoor;
        var vent = GridTerminalSystem.GetBlockWithName(VENT) as IMyAirVent;

        if (action == null)
        {
            action = "enable_doors";
        }

        if (action.Equals("enable_doors"))
        {
            enableDoor(door_inside);
            enableDoor(door_outside);
            action = "close_doors";
            timer = 0;
            nextTick();
            return;
        }

        if (action.Equals("close_doors"))
        {
            closeDoor(door_outside);
            closeDoor(door_inside);
            state = "close_doors";
            timer = 0;
            if (door_outside.Open)
            {
                action = "out_to_in";
            }
            else
            {
                action = "in_to_out";
            }
        }

        if (action.Equals("disable_doors"))
        {
            if (timer >= DOOR_ANIMATION_DURATION)
            {
                door_inside.GetActionWithName(DISABLE).Apply(door_inside);
                door_outside.GetActionWithName(DISABLE).Apply(door_outside);
                timer = 0;
                action = null;
                return;
            }
            nextTick();
            return;
        }

        if (action.Equals("in_to_out"))
        {
            if (state.Equals("close_doors"))
            {
                if (timer >= DOOR_ANIMATION_DURATION)
                {
                    vent.GetActionWithName(ENABLE).Apply(vent);
                    vent.GetActionWithName(DE_PRESSURIZE).Apply(vent);
                    enableDoor(door_outside);
                    state = "depressurizing";
                    timer = 0;
                }
            }
            else if (state.Equals("depressurizing"))
            {
                if (vent.GetOxygenLevel() < 0.00001 || timer >= MAX_WAIT_DEPRESSURIZING * 60)
                {
                    vent.GetActionWithName(DISABLE).Apply(vent);
                    openDoor(door_outside);
                    action = DISABLE_DOORS ? "disable_doors" : null;
                    timer = 0;
                    state = null;
                }
            }
        }
        else if (action.Equals("out_to_in"))
        {
            if (state.Equals("close_doors"))
            {
                if (timer >= DOOR_ANIMATION_DURATION)
                {
                    vent.GetActionWithName(ENABLE).Apply(vent);
                    vent.GetActionWithName(PRESSURIZE).Apply(vent);
                    enableDoor(door_inside);
                    state = "pressurizing";
                    timer = 0;
                }
            }
            else if (state.Equals("pressurizing"))
            {
                if (vent.GetOxygenLevel() > 0.99999 || timer > MAX_WAIT_PRESSURIZING * 60)
                {
                    vent.GetActionWithName(DISABLE).Apply(vent);
                    openDoor(door_inside);
                    action = DISABLE_DOORS ? "disable_doors" : null;
                    timer = 0;
                    state = null;
                }
            }
        }

        if (action != null || state != null)
        {
            nextTick();
        }
    }
    #endregion
}
