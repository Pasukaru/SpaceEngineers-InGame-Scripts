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

    //Name of the Timer block (Set the first action of the Timer Block to this Programmable Block->Run) 
    const string TIMER_BLOCK = "TBTEST";

    const long WAIT_FOR_DOOR = 1000;

    //Set this to false, if you don't want the PB to power off doors (prevents manual open/close and accidental loss of air) 
    const bool DISABLE_DOORS = true;

    //***** DO NOT MODIFY BELOW THIS LINE *****\\       

    const string OPEN_DOOR = "Open_On";
    const string CLOSE_DOOR = "Open_Off";

    const string ENABLE = "OnOff_On";
    const string DISABLE = "OnOff_Off";

    const string DE_PRESSURIZE = "Depressurize_On";
    const string PRESSURIZE = "Depressurize_Off";

    const string RUN = "Run";

    void nextTick()
    {
        var tb = GridTerminalSystem.GetBlockWithName(TIMER_BLOCK) as IMyTimerBlock;
        tb.GetActionWithName("TriggerNow").Apply(tb);
    }

    void log(string message)
    {
        var groups = GridTerminalSystem.BlockGroups;
        List<IMyTerminalBlock> group = null;
        for (int i = 0; i < groups.Count; i++)
        {
            if (groups[i].Name == "Antenna")
            {
                group = groups[i].Blocks;
                break;
            }
        }

        string a = _action == null ? "null" : _action.GetType().Name;
        group[0].SetCustomName(a + ": " + message);
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

    interface Wait
    {
        bool wait();
    }

    class WaitForPressure : Wait
    {
        private IMyAirVent vent;
        private bool up;

        private long start = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

        public WaitForPressure(IMyAirVent vent, bool up)
        {
            this.vent = vent;
            this.up = up;
        }

        public bool wait()
        {
            var ox = vent.GetOxygenLevel();
            if (up)
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
            long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            return now - start >= 3000;
        }
    }

    class WaitForMs : Wait
    {
        private readonly long start = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        private readonly long ms;

        public WaitForMs(long ms)
        {
            this.ms = ms;
        }

        public bool wait()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - start >= 1000;
        }
    }

    Wait wait = null;
    Action _action = null;

    abstract class Action
    {
        private Action action = null;

        public Action Then(Action nextAction)
        {
            this.action = nextAction;
            return NextAction();
        }

        public Action NextAction()
        {
            return action;
        }

        public abstract Wait Run();
    }

    abstract class DoorsAction : Action
    {
        protected readonly IMyDoor[] doors;
        public DoorsAction(params IMyDoor[] doors)
        {
            this.doors = doors;
        }
    }

    class EnableDoors : DoorsAction
    {
        public EnableDoors(params IMyDoor[] doors) : base(doors) { }

        public override Wait Run()
        {
            foreach (IMyDoor door in doors)
            {
                door.ApplyAction(ENABLE);
            }
            return null;
        }
    }

    class DisableDoors : DoorsAction
    {
        public DisableDoors(params IMyDoor[] doors) : base(doors) { }

        public override Wait Run()
        {
            foreach (IMyDoor door in doors)
            {
                door.ApplyAction(DISABLE);
            }
            return null;
        }
    }

    class ToggleDoors : DoorsAction
    {
        protected readonly string a;
        public ToggleDoors(string action, params IMyDoor[] doors)
            : base(doors)
        {
            this.a = action;
        }

        public override Wait Run()
        {
            foreach (IMyDoor door in doors)
            {
                door.GetActionWithName(a).Apply(door);
            }
            return new WaitForMs(WAIT_FOR_DOOR);
        }
    }

    class PressureAction : Action
    {
        private readonly IMyAirVent vent;
        private readonly bool pressurize;
        public PressureAction(IMyAirVent vent, bool pressurize)
        {
            this.vent = vent;
            this.pressurize = pressurize;
        }

        public override Wait Run()
        {
            if (pressurize)
            {
                vent.GetActionWithName(PRESSURIZE).Apply(vent);
                return new WaitForPressure(vent, true);
            }
            else
            {
                vent.GetActionWithName(DE_PRESSURIZE).Apply(vent);
                return new WaitForPressure(vent, false);
            }
        }
    }

    void Main()
    {
        if (wait != null)
        {
            log("Waiting... (next");
            if (!wait.wait())
            {
                nextTick();
                return;
            }
            wait = null;
            if (_action == null)
            {
                return;
            }
        }

        if (_action == null)
        {
            var door_outside = GridTerminalSystem.GetBlockWithName(DOOR_OUTSIDE) as IMyDoor;
            var door_inside = GridTerminalSystem.GetBlockWithName(DOOR_INSIDE) as IMyDoor;
            var vent = GridTerminalSystem.GetBlockWithName(VENT) as IMyAirVent;

            if (door_outside.Open)
            {
                _action = new EnableDoors(door_outside, door_inside);
                _action.Then(new ToggleDoors(CLOSE_DOOR, door_outside, door_inside))
                    .Then(new PressureAction(vent, true))
                    .Then(new ToggleDoors(OPEN_DOOR, door_inside))
                    .Then(new DisableDoors(door_outside, door_inside));
            }
            else
            {
                _action = new EnableDoors(door_outside, door_inside);
                _action.Then(new ToggleDoors(CLOSE_DOOR, door_outside, door_inside))
                    .Then(new PressureAction(vent, false))
                    .Then(new ToggleDoors(OPEN_DOOR, door_outside))
                    .Then(new DisableDoors(door_outside, door_inside));
            }
        }

        wait = _action.Run();
        _action = _action.NextAction();
        if (_action != null)
        {
            nextTick();
        }

    }
    #endregion
}
