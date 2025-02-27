﻿module Gamepad

open System
open SharpDX.DirectInput

let mutable private LeftShoulderButtonIsDown = 0

let mutable LeftShoulderButtonMostRecentRelease = DateTime.Now   // lack of threadsafety might make it inaccurate/stale, but that's ok

let IsLeftShoulderButtonDown() =
    System.Threading.Volatile.Read(&LeftShoulderButtonIsDown) = 1

let Initialize() =
    let mutable joystickGuid = Guid.Empty
    let directInput = new DirectInput()
    for deviceInstance in directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices) do
        joystickGuid <- deviceInstance.InstanceGuid
    if joystickGuid = Guid.Empty then
        for deviceInstance in directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices) do
            joystickGuid <- deviceInstance.InstanceGuid
    if joystickGuid = Guid.Empty then
        printfn "gamepad not found"
        false
    else
        printfn "found gamepad"

        let joystick = new Joystick(directInput, joystickGuid)
        // Set BufferSize in order to use buffered data.
        joystick.Properties.BufferSize <- 128
        // Acquire the joystick
        joystick.Acquire()

        async {
            // Poll events from joystick
            while true do
                joystick.Poll()
                let datas = joystick.GetBufferedData()
                for state in datas do
                    if state.Offset = JoystickOffset.Buttons4 then
                        if state.Value = 128 then
                            //printfn "left shoulder button pressed"
                            System.Threading.Volatile.Write(&LeftShoulderButtonIsDown,1)
                        if state.Value = 0 then
                            //printfn "left shoulder button released"
                            System.Threading.Volatile.Write(&LeftShoulderButtonIsDown,0)
                            LeftShoulderButtonMostRecentRelease <- DateTime.Now
                    //printfn "%s" (state.ToString())
        } |> Async.Start
        true

