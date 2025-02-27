﻿module TrackerModel

// model to track the state of the tracker independent of any UI/graphics layer

//////////////////////////////////////////////////////////////////////////////////////////
// Options

open System.Text.Json
open System.Text.Json.Serialization

[<RequireQualifiedAccess>]
type ReminderCategory =
    | DungeonFeedback
    | SwordHearts
    | CoastItem
    | RecorderPBSpotsAndBoomstickBook
    | HaveKeyLadder
    | Blockers

type IntentionalApplicationShutdown(msg) =
    inherit System.Exception(msg)

module Options =
    type Bool(init) =
        let mutable v = init
        member this.Value with get() = v and set(x) = v <- x
    module Overworld =
        let mutable DrawRoutes = Bool(true)
        let mutable HighlightNearby = Bool(true)
        let mutable ShowMagnifier = Bool(true)
        let mutable MirrorOverworld = Bool(false)
        let mutable ShopsFirst = Bool(true)
    module VoiceReminders =
        let mutable DungeonFeedback = Bool(true)
        let mutable SwordHearts = Bool(true)
        let mutable CoastItem = Bool(true)
        let mutable RecorderPBSpotsAndBoomstickBook = Bool(false)
        let mutable HaveKeyLadder = Bool(true)
        let mutable Blockers = Bool(true)
    module VisualReminders =
        let mutable DungeonFeedback = Bool(true)
        let mutable SwordHearts = Bool(true)
        let mutable CoastItem = Bool(true)
        let mutable RecorderPBSpotsAndBoomstickBook = Bool(false)
        let mutable HaveKeyLadder = Bool(true)
        let mutable Blockers = Bool(true)
    let mutable ListenForSpeech = Bool(true)
    let mutable RequirePTTForSpeech = Bool(false)
    let mutable PlaySoundWhenUseSpeech = Bool(true)
    let mutable IsSecondQuestDungeons = Bool(false)
    let mutable ShowBroadcastWindow = Bool(false)
    let mutable BroadcastWindowSize = 3
    let mutable BroadcastWindowIncludesOverworldMagnifier = Bool(false)
    let mutable SmallerAppWindow = Bool(false)
    let mutable IsMuted = false
    let mutable Volume = 30

    type ReadWrite() =
        member val DrawRoutes = true with get,set
        member val HighlightNearby = true with get,set
        member val ShowMagnifier = true with get,set
        member val MirrorOverworld = false with get,set
        member val ShopsFirst = false with get,set

        member val Voice_DungeonFeedback = true with get,set
        member val Voice_SwordHearts = true with get,set
        member val Voice_CoastItem = true with get,set
        member val Voice_RecorderPBSpotsAndBoomstickBook = true with get,set
        member val Voice_HaveKeyLadder = true with get,set
        member val Voice_Blockers = true with get,set

        member val Visual_DungeonFeedback = true with get,set
        member val Visual_SwordHearts = true with get,set
        member val Visual_CoastItem = true with get,set
        member val Visual_RecorderPBSpotsAndBoomstickBook = true with get,set
        member val Visual_HaveKeyLadder = true with get,set
        member val Visual_Blockers = true with get,set
        
        member val ListenForSpeech = true with get,set
        member val RequirePTTForSpeech = false with get,set
        member val PlaySoundWhenUseSpeech = true with get,set
        member val IsSecondQuestDungeons = false with get,set
        member val ShowBroadcastWindow = false with get,set
        member val BroadcastWindowSize = 3 with get,set
        member val BroadcastWindowIncludesOverworldMagnifier = false with get,set
        member val SmallerAppWindow = false with get,set

        member val IsMuted = false with get, set
        member val Volume = 30 with get, set

    let mutable private cachedSettingJson = null

    let private writeImpl(filename) =
        let data = ReadWrite()
        data.DrawRoutes <- Overworld.DrawRoutes.Value
        data.HighlightNearby <- Overworld.HighlightNearby.Value
        data.ShowMagnifier <- Overworld.ShowMagnifier.Value
        data.MirrorOverworld <- Overworld.MirrorOverworld.Value
        data.ShopsFirst <- Overworld.ShopsFirst.Value

        data.Voice_DungeonFeedback <- VoiceReminders.DungeonFeedback.Value
        data.Voice_SwordHearts <-     VoiceReminders.SwordHearts.Value
        data.Voice_CoastItem <-       VoiceReminders.CoastItem.Value
        data.Voice_RecorderPBSpotsAndBoomstickBook <- VoiceReminders.RecorderPBSpotsAndBoomstickBook.Value
        data.Voice_HaveKeyLadder <-   VoiceReminders.HaveKeyLadder.Value
        data.Voice_Blockers <-        VoiceReminders.Blockers.Value

        data.Visual_DungeonFeedback <- VisualReminders.DungeonFeedback.Value
        data.Visual_SwordHearts <-     VisualReminders.SwordHearts.Value
        data.Visual_CoastItem <-       VisualReminders.CoastItem.Value
        data.Visual_RecorderPBSpotsAndBoomstickBook <- VisualReminders.RecorderPBSpotsAndBoomstickBook.Value
        data.Visual_HaveKeyLadder <-   VisualReminders.HaveKeyLadder.Value
        data.Visual_Blockers <-        VisualReminders.Blockers.Value

        data.ListenForSpeech <- ListenForSpeech.Value
        data.RequirePTTForSpeech <- RequirePTTForSpeech.Value
        data.PlaySoundWhenUseSpeech <- PlaySoundWhenUseSpeech.Value
        data.IsSecondQuestDungeons <- IsSecondQuestDungeons.Value
        data.ShowBroadcastWindow <- ShowBroadcastWindow.Value
        data.BroadcastWindowSize <- BroadcastWindowSize
        data.BroadcastWindowIncludesOverworldMagnifier <- BroadcastWindowIncludesOverworldMagnifier.Value
        data.SmallerAppWindow <- SmallerAppWindow.Value
        data.IsMuted <- IsMuted
        data.Volume <- Volume

        let json = JsonSerializer.Serialize<ReadWrite>(data, new JsonSerializerOptions(WriteIndented=true))
        if json <> cachedSettingJson then
            cachedSettingJson <- json
            System.IO.File.WriteAllText(filename, cachedSettingJson)

    let private write(filename) =
        try
            writeImpl(filename)
        with e ->
            printfn "failed to write settings file '%s':" filename
            printfn "%s" (e.ToString())
            printfn ""

    let private read(filename) =
        try
            cachedSettingJson <- System.IO.File.ReadAllText(filename)
            let data = JsonSerializer.Deserialize<ReadWrite>(cachedSettingJson, new JsonSerializerOptions(AllowTrailingCommas=true))
            Overworld.DrawRoutes.Value <- data.DrawRoutes
            Overworld.HighlightNearby.Value <- data.HighlightNearby
            Overworld.ShowMagnifier.Value <- data.ShowMagnifier
            Overworld.MirrorOverworld.Value <- data.MirrorOverworld
            Overworld.ShopsFirst.Value <- data.ShopsFirst    

            VoiceReminders.DungeonFeedback.Value <- data.Voice_DungeonFeedback
            VoiceReminders.SwordHearts.Value <-     data.Voice_SwordHearts
            VoiceReminders.CoastItem.Value <-       data.Voice_CoastItem
            VoiceReminders.RecorderPBSpotsAndBoomstickBook.Value <- data.Voice_RecorderPBSpotsAndBoomstickBook
            VoiceReminders.HaveKeyLadder.Value <-   data.Voice_HaveKeyLadder
            VoiceReminders.Blockers.Value <-        data.Voice_Blockers

            VisualReminders.DungeonFeedback.Value <- data.Visual_DungeonFeedback
            VisualReminders.SwordHearts.Value <-     data.Visual_SwordHearts
            VisualReminders.CoastItem.Value <-       data.Visual_CoastItem
            VisualReminders.RecorderPBSpotsAndBoomstickBook.Value <- data.Visual_RecorderPBSpotsAndBoomstickBook
            VisualReminders.HaveKeyLadder.Value <-   data.Visual_HaveKeyLadder
            VisualReminders.Blockers.Value <-        data.Visual_Blockers

            ListenForSpeech.Value <- data.ListenForSpeech
            RequirePTTForSpeech.Value <- data.RequirePTTForSpeech
            PlaySoundWhenUseSpeech.Value <- data.PlaySoundWhenUseSpeech
            IsSecondQuestDungeons.Value <- data.IsSecondQuestDungeons
            ShowBroadcastWindow.Value <- data.ShowBroadcastWindow
            BroadcastWindowSize <- max 1 (min 3 data.BroadcastWindowSize)
            BroadcastWindowIncludesOverworldMagnifier.Value <- data.BroadcastWindowIncludesOverworldMagnifier
            SmallerAppWindow.Value <- data.SmallerAppWindow
            IsMuted <- data.IsMuted
            Volume <- max 0 (min 100 data.Volume)
        with e ->
            cachedSettingJson <- null
            printfn "Unable to read settings file '%s':" filename 
            if System.IO.File.Exists(filename) then
                printfn "That file does exist on disk, but could not be read.  Here is the error detail:"
                printfn "%s" (e.ToString())
                printfn ""
                printfn "If you were intentionally hand-editing the .json settings file, you may have made a mistake that must be corrected."
                printfn ""
                printfn "The application will shut down now. If you want to discard the broken settings file and start %s fresh with some default settings, then simply delete the file:" OverworldData.ProgramNameString
                printfn ""
                printfn "%s" filename
                printfn ""
                printfn "from disk before starting the application again."
                printfn ""
                raise <| IntentionalApplicationShutdown "Intentional application shutdown: failed to read settings file data."
            else
                printfn "Perhaps this is your first time using '%s' on this machine?" OverworldData.ProgramNameString
                printfn "A default settings file will be created for you..."
                try
                    writeImpl(filename)
                    printfn "... The settings file has been successfully created."
                with e ->
                    printfn "... Failed to write settings file.  Perhaps there is an issue with the file location?"
                    printfn "'Filename'='%s'" filename
                    printfn "Here is more information about the problem:"
                    printfn "%s" (e.ToString())
                    printfn ""

    let mutable private settingsFile = null
    
    let readSettings() =
        if settingsFile = null then
            settingsFile <- System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Z1R_Tracker_settings.json")
        read(settingsFile)
    let writeSettings() =
        if settingsFile = null then
            settingsFile <- System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Z1R_Tracker_settings.json")
        write(settingsFile)

///////////////////////////////////////////////////////////////////////////

// abstraction for a set of scrollable choices
[<AllowNullLiteral>]
type ChoiceDomain(name:string,maxUsesArray:int[]) =
    // index keys are integers used as identifiers, values are max number of times it can appear among a set of cells
    // examples: ladder has max use of 1, heart container has max use of 9 in heartShuffle mode, bomb shop has max use of 999 (infinity)
    let name = name
    let maxUsesArray = Array.copy maxUsesArray
    let uses = Array.zeroCreate maxUsesArray.Length
    let ev = new Event<_>()
    [<CLIEvent>]
    member _this.Changed = ev.Publish
    member _this.Name = name  // just useful for debugging etc
    member _this.MaxKey = uses.Length-1
    member this.RemoveUse(key) =
        if key = -1 then  // since Next/Prev FreeKey can return -1 to mean the implicit empty slot, seems ok to RemoveUse() it, it implicitly has infinity current uses
            ev.Trigger(this,key)
        else
            if uses.[key] > 0 then
                uses.[key] <- uses.[key] - 1
                ev.Trigger(this,key)
            else
                failwith "choice domain underflow"
    member this.AddUse(key) =
        if key = -1 then  // since Next/Prev FreeKey can return -1 to mean the implicit empty slot, seems ok to AddUse() it, it implicitly has infinity max uses
            ev.Trigger(this,key)
        else
            if uses.[key] >= maxUsesArray.[key] then
                failwith "choice domain overflow"
            else
                uses.[key] <- uses.[key] + 1
                ev.Trigger(this,key)
    member this.NextFreeKeyWithAllowance(key, allowance) =
        if key = uses.Length-1 then
            -1
        elif (allowance = key+1) || (uses.[key+1] < maxUsesArray.[key+1]) then
            key+1
        else
            this.NextFreeKeyWithAllowance(key+1, allowance)
    member this.NextFreeKey(key) = this.NextFreeKeyWithAllowance(key, -999)  
    member this.PrevFreeKeyWithAllowance(key, allowance) =
        if key = -1 then
            this.PrevFreeKeyWithAllowance(uses.Length, allowance)
        elif key = 0 then
            -1
        elif (allowance = key-1) || (uses.[key-1] < maxUsesArray.[key-1]) then
            key-1
        else
            this.PrevFreeKeyWithAllowance(key-1, allowance)
    member this.PrevFreeKey(key) = this.PrevFreeKeyWithAllowance(key, -999)  
    member _this.NumUses(key) = uses.[key]
    member _this.MaxUses(key) = maxUsesArray.[key]
    member _this.CanAddUse(key) = (key = -1) || (uses.[key] < maxUsesArray.[key])
        
type Cell(cd:ChoiceDomain) =
    // a location that can hold one item, e.g. armos box that can hold red candle or white sword or whatnot, or
    // map square that can hold dungeon 4 or bomb shop or whatnot
    // this is about player-knowing-location-contents, not about players _having_ the things there
    let mutable state = -1 // -1 means empty, 0-N are item identifiers
    member _this.Current() = state
    member this.Next() =
        if state <> -1 then
            cd.RemoveUse(state)
        state <- cd.NextFreeKey(state)
        if state <> -1 then
            cd.AddUse(state)
    member this.Prev() =
        if state <> -1 then
            cd.RemoveUse(state)
        state <- cd.PrevFreeKey(state)
        if state <> -1 then
            cd.AddUse(state)
    member this.Set(newState) =
        if newState < -1 || newState > cd.MaxKey then
            failwith "Cell.Set out of range"
        if state = newState then
            ()
        else
            cd.AddUse(newState)
            cd.RemoveUse(state)
            state <- newState
    member this.AttemptToSet(newState) =
        try
            this.Set(newState)
            true
        with _e -> 
            false

//////////////////////////////////////////////////////////////////////////////////////////

let IsCurrentlyBook, ToggleIsCurrentlyBook, IsCurrentlyBookChanged = 
    let mutable isCurrentlyBook = true   // false if this is a boomstick seed
    let isCurrentlyBookChangeEvent = new Event<_>()
    let IsCurrentlyBook() = isCurrentlyBook
    let ToggleIsCurrentlyBook() = isCurrentlyBook <- not isCurrentlyBook; isCurrentlyBookChangeEvent.Trigger(isCurrentlyBook)
    let IsCurrentlyBookChanged = isCurrentlyBookChangeEvent.Publish
    IsCurrentlyBook, ToggleIsCurrentlyBook, IsCurrentlyBookChanged

module ITEMS =
    let itemNamesAndCounts = [|
        "BookOrShield"      , 1
        "Boomerang"         , 1
        "Bow"               , 1
        "PowerBracelet"     , 1
        "Ladder"            , 1
        "MagicBoomerang"    , 1
        "AnyKey"            , 1
        "Raft"              , 1
        "Recorder"          , 1
        "RedCandle"         , 1
        "RedRing"           , 1
        "SilverArrow"       , 1
        "Wand"              , 1
        "WhiteSword"        , 1
        "HeartContainer"    , 9
        |]
    let BOOKSHIELD = 0
    let BOOMERANG = 1
    let BOW = 2
    let POWERBRACELET = 3
    let LADDER = 4
    let MAGICBOOMERANG = 5
    let KEY = 6
    let RAFT = 7
    let RECORDER = 8
    let REDCANDLE = 9
    let REDRING = 10
    let SILVERARROW = 11
    let WAND = 12
    let WHITESWORD = 13
    let HEARTCONTAINER = 14
    let AsPronounceString(n) =
        match n with
        | 0 -> if IsCurrentlyBook() then "book" else "shield"
        | 1 -> "boomerang"
        | 2 -> "beau"
        | 3 -> "power bracelet"
        | 4 -> "ladder"
        | 5 -> "magic boomerang"
        | 6 -> "any key"
        | 7 -> "raft"
        | 8 -> "recorder"
        | 9 -> "red candle"
        | 10 -> "red ring"
        | 11 -> "silver arrow"
        | 12 -> "wand"
        | 13 -> "white sword"
        | 14 -> "heart container"
        | _ -> failwith "bad ITEMS id"
    let AsHotKeyName(n) =
        if n>=0 && n<itemNamesAndCounts.Length then
            fst(itemNamesAndCounts.[n])
        else
            failwith "bad ITEMS id"
    let AsDisplayDescription(n) =
        match n with
        | 0 -> if IsCurrentlyBook() then "Book" else "Magical Shield"
        | 1 -> "Wooden Boomerang"
        | 2 -> "Bow"
        | 3 -> "Power Bracelet"
        | 4 -> "Ladder"
        | 5 -> "Magic Boomerang"
        | 6 -> "Any Key"
        | 7 -> "Raft"
        | 8 -> "Recorder"
        | 9 -> "Red Candle"
        | 10 -> "Red Ring"
        | 11 -> "Silver Arrow"
        | 12 -> "Wand"
        | 13 -> "White Sword"
        | 14 -> "Heart Container"
        | _ -> failwith "bad ITEMS id"
let allItemWithHeartShuffleChoiceDomain = ChoiceDomain("allItemsWithHeartShuffle", ITEMS.itemNamesAndCounts |> Array.map snd)

//////////////////////////////////////////////////////////////////////////////////////////

let overworldTiles(isFirstQuestOverworld) = [|
    // hotkey name       maxuses                                              popup display text
    "Level1"           , 1                                                  , "Dungeon"
    "Level2"           , 1                                                  , "Dungeon"
    "Level3"           , 1                                                  , "Dungeon"
    "Level4"           , 1                                                  , "Dungeon"
    "Level5"           , 1                                                  , "Dungeon"
    "Level6"           , 1                                                  , "Dungeon"
    "Level7"           , 1                                                  , "Dungeon"
    "Level8"           , 1                                                  , "Dungeon"
    "Level9"           , 1                                                  , "Final Dungeon"
    "AnyRoad1"         , 1                                                  , "Any Road 1/4"
    "AnyRoad2"         , 1                                                  , "Any Road 2/4"
    "AnyRoad3"         , 1                                                  , "Any Road 3/4"
    "AnyRoad4"         , 1                                                  , "Any Road 4/4"
    "Sword3"           , 1                                                  , "Magical Sword Cave"
    "Sword2"           , 1                                                  , "White Sword Cave"
    "Sword1"           , 1                                                  , "Wood Sword Cave"
    // 1Q has 12 shops, distributed 4,4,3,1                                
    // 2Q has 15 shops, distributed 6,4,4,1     (4 kinds of shops)         
    "ArrowShop"        , 999                                                , "Shop with\nWood Arrows\n(60-100 rupees)"
    "BombShop"         , 999                                                , "Shop with\n4 Bomb Pack\n(1-40 rupees)"
    "BookShop"         , 999                                                , "Shop with\n(Boomstick) Book\n(180-220 rupees)"
    "CandleShop"       , 999                                                , "Shop with\nBlue Candle\n(40-80 rupees)"
    "BlueRingShop"     , 999                                                , "Shop with\nBlue Ring\n(230-255 rupees)"
    "MeatShop"         , 999                                                , "Shop with\nMeat\n(40-120 rupees)"
    "KeyShop"          , 999                                                , "Shop with\nKey\n(60-120 rupees)"
    "ShieldShop"       , 999                                                , "Shop with\nMagical Shield\n(70-180 rupees)"
    "UnknownSecret"    , 999                                                , "Unknown Secret"
    "LargeSecret"      , 999 (*if isFirstQuestOverworld then 3 else 1*)     , "Large Secret\n(50-150 rupees)"
    "MediumSecret"     , 999 (*if isFirstQuestOverworld then 7 else 7*)     , "Medium Secret\n(25-40 rupees)"
    "SmallSecret"      , 999 (*if isFirstQuestOverworld then 4 else 6*)     , "Small Secret\n(1-20 rupees)"
    "DoorRepairCharge" , (if isFirstQuestOverworld then 9 else 10)          , "Door Repair Charge\n(15-25 rupees)"
    "MoneyMakingGame"  , (if isFirstQuestOverworld then 5 else 6)           , "Money Making Game\n(gambling)"
    "Letter"           , 1                                                  , "The Letter\n(for buying potions)"
    "Armos"            , 1                                                  , "Armos Item"
    // white/magical sword cave hint may also be marked as                 
    // a free hint 'shop', so 4 rather than 2                              
    "HintShop"         , 4                                                  , "Hint Shop\n(10-60 rupees each)\nor free hint for\nwhite/magical\nsword cave"
    "TakeAny"          , 4                                                  , "Take Any One\nYou Want"
    "PotionShop"       , (if isFirstQuestOverworld then 7 else 9)           , "Potion Shop\n(20-60, 48-88 rupees)"
    "DarkX"            , 999                                                , "Don't Care"
    |]   // 1Q has 73 total spots, 2Q has 80
let dummyOverworldTiles = overworldTiles(true)  // some bits need to read the hotkey names or array length, before the 1Q/2Q choice has been made by the user, this gives them that info

let mutable mapSquareChoiceDomain = null : ChoiceDomain
// Note: if you make changes to above/below, also check: recomputeMapStateSummary(), Graphics.theInteriorBmpTable, SpeechRecognition, OverworldMapTileCustomization, ui's isLegalHere()
type MapSquareChoiceDomainHelper = 
    static member DUNGEON_1 = 0
    static member DUNGEON_2 = 1
    static member DUNGEON_3 = 2
    static member DUNGEON_4 = 3
    static member DUNGEON_5 = 4
    static member DUNGEON_6 = 5
    static member DUNGEON_7 = 6
    static member DUNGEON_8 = 7
    static member DUNGEON_9 = 8
    static member WARP_1 = 9
    static member WARP_2 = 10
    static member WARP_3 = 11
    static member WARP_4 = 12
    static member SWORD3 = 13
    static member SWORD2 = 14
    static member SWORD1 = 15
    // item shop stuff
    static member ARROW = 16
    static member BOMB = 17
    static member BOOK = 18
    static member BLUE_CANDLE = 19
    static member BLUE_RING = 20
    static member MEAT = 21
    static member KEY = 22
    static member SHIELD = 23
    static member SHOP = MapSquareChoiceDomainHelper.ARROW  // key into extra-data store for all shops
    static member NUM_ITEMS = 8 // 8 possible types of items can be tracked, listed above
    static member IsItem(state) = state >= 16 && state <= 23
    static member ToItem(state) = if MapSquareChoiceDomainHelper.IsItem(state) then state-15 else 0   // format used by TrackerModel.overworldMapExtraData
    // other stuff
    static member UNKNOWN_SECRET = 24
    static member LARGE_SECRET = 25
    static member MEDIUM_SECRET = 26
    static member SMALL_SECRET = 27
    static member DOOR_REPAIR_CHARGE = 28
    static member MONEY_MAKING_GAME = 29
    static member THE_LETTER = 30
    static member ARMOS = 31
    static member HINT_SHOP = 32
    static member TAKE_ANY = 33
    static member POTION_SHOP = 34
    static member DARK_X = 35
    static member AsHotKeyName(n) =
        if n>=0 && n<dummyOverworldTiles.Length then
            let r,_,_ = dummyOverworldTiles.[n] in r
        else
            failwith "bad overworld tile id"

//////////////////////////////////////////////////////////////////////////////////////////

// not threadsafe
type LastChangedTime(intervalHowFarInThePast) as this =
    static let mutable pause = None
    static let allInstances = ResizeArray()
    let mutable stamp = System.DateTime.Now - intervalHowFarInThePast
    let mutable isFuture = None  // if Some(interval), interval is how far in past upon waking
    do
        allInstances.Add(this)
    static member private CurrentPause = pause
    static member IsPaused = pause.IsSome
    static member PauseAll() = 
        match pause with
        | Some _ -> ()
        | None -> pause <- Some(System.DateTime.Now)
    static member ResumeAll() =
        match pause with
        | None -> ()
        | Some whenPaused ->
            let now = System.DateTime.Now
            let interval = now - whenPaused
            for lct in allInstances do
                lct.AddTime(interval, now)
            pause <- None
    new() = LastChangedTime(System.TimeSpan.Zero)
    member this.SetNow() = 
        stamp <- System.DateTime.Now
        match LastChangedTime.CurrentPause with
        | None -> ()
        | Some _ -> isFuture <- Some(System.TimeSpan.Zero)
    member this.SetAgo(interval) = 
        stamp <- System.DateTime.Now - interval
        match LastChangedTime.CurrentPause with
        | None -> ()
        | Some _ -> isFuture <- Some(interval)
    member private this.AddTime(interval, now) =
        match isFuture with
        | None -> stamp <- stamp + interval
        | Some(howFarToGoBack) ->
            stamp <- now - howFarToGoBack
            isFuture <- None
    member this.Time = 
        match LastChangedTime.CurrentPause with
        | None -> stamp
        | Some whenPaused -> 
            match isFuture with
            | Some(interval) -> System.DateTime.Now - interval
            | None -> stamp + (System.DateTime.Now - whenPaused)

//////////////////////////////////////////////////////////////////////////////////////////

(*
Two main kinds of UI changes can occur:
 - user can scroll/change a Cell or click a Box, which requires immediate visual feedback
 - user can change model state requiring model to be recomputed, where feedback need not be immediate
     - indeed, 'buffering' these changes can be good, as scrolling an item often begets another scroll (need to scroll through many to find item user wants)

Thus, UI-driven live changes to UI do not need change events - if UI scrolls, UI is responsible to redraw the scrolled Cell right now.

And model changes need to just record some sub-portion of the model which has become stale, and then have some policy by which after some time, the entire model
gets recomputed and a fresh model is published via change events to the UI for a display update.

In practice, chose to divide state into a few main groups, which keep of when they last changed, and then have simple dependencies to recompute stale data and bring entire model back up to date.

Model is not enitrely threadsafe; will do all these computations on the UI thread.
*)


//////////////////////////////////////////////////////////////////////////////////////////
// Player Progress

type BoolProperty(initState,changedFunc) =
    let mutable state = initState
    let changed = new Event<_>()
    member _this.Set(b) =
        state <- b
        changedFunc()
        changed.Trigger(state)
    member _this.Toggle() =
        state <- not state
        changedFunc()
        changed.Trigger(state)
    member _this.Value() = state
    member _this.Changed = changed.Publish
let playerProgressLastChangedTime = new LastChangedTime()
type PlayerProgressAndTakeAnyHearts() =
    // describes the state directly accessible in the upper right portion of the UI
    let takeAnyHearts = [| 0; 0; 0; 0 |]   // 0 = untaken (open heart on UI), 1 = taken heart (red heart on UI), 2 = taken potion/candle (X out empty heart on UI)
    let playerHasBoomBook      = BoolProperty(false,fun()->playerProgressLastChangedTime.SetNow())
    let playerHasWoodSword     = BoolProperty(false,fun()->playerProgressLastChangedTime.SetNow())
    let playerHasWoodArrow     = BoolProperty(false,fun()->playerProgressLastChangedTime.SetNow())
    let playerHasBlueRing      = BoolProperty(false,fun()->playerProgressLastChangedTime.SetNow())
    let playerHasBlueCandle    = BoolProperty(false,fun()->playerProgressLastChangedTime.SetNow())
    let playerHasMagicalSword  = BoolProperty(false,fun()->playerProgressLastChangedTime.SetNow())
    let playerHasDefeatedGanon = BoolProperty(false,fun()->playerProgressLastChangedTime.SetNow())
    let playerHasRescuedZelda  = BoolProperty(false,fun()->playerProgressLastChangedTime.SetNow())
    let playerHasBombs         = BoolProperty(false,fun()->playerProgressLastChangedTime.SetNow())
    let takeAnyHeartChanged = new Event<_>()
    member _this.GetTakeAnyHeart(i) = takeAnyHearts.[i]
    member _this.SetTakeAnyHeart(i,v) = 
        takeAnyHearts.[i] <- v
        playerProgressLastChangedTime.SetNow()
        takeAnyHeartChanged.Trigger(i)
    member _this.TakeAnyHeartChanged = takeAnyHeartChanged.Publish
    member _this.PlayerHasBoomBook      = playerHasBoomBook
    member _this.PlayerHasWoodSword     = playerHasWoodSword     
    member _this.PlayerHasWoodArrow     = playerHasWoodArrow
    member _this.PlayerHasBlueRing      = playerHasBlueRing      
    member _this.PlayerHasBlueCandle    = playerHasBlueCandle    
    member _this.PlayerHasMagicalSword  = playerHasMagicalSword  
    member _this.PlayerHasDefeatedGanon = playerHasDefeatedGanon 
    member _this.PlayerHasRescuedZelda  = playerHasRescuedZelda  
    member _this.PlayerHasBombs         = playerHasBombs
let playerProgressAndTakeAnyHearts = PlayerProgressAndTakeAnyHearts()

//////////////////////////////////////////////////////////////////////////////////////////
// Dungeons and Boxes

let dungeonsAndBoxesLastChangedTime = new LastChangedTime()
[<RequireQualifiedAccess>]
type PlayerHas = | YES | NO | SKIPPED
type Box() =
    // this contains both a Cell (player-knowing-location-contents), and a bool (whether the players _has_ the thing there)
    let cell = new Cell(allItemWithHeartShuffleChoiceDomain)
    let mutable playerHas = PlayerHas.NO
    let changed = new Event<_>()
    member _this.Changed = changed.Publish
    member _this.PlayerHas() = playerHas
    member _this.CellNextFreeKey() = allItemWithHeartShuffleChoiceDomain.NextFreeKey(cell.Current())
    member _this.CellPrevFreeKey() = allItemWithHeartShuffleChoiceDomain.PrevFreeKey(cell.Current())
    member _this.CellPrev() = 
        cell.Prev()
        dungeonsAndBoxesLastChangedTime.SetNow()
        changed.Trigger()
    member _this.CellNext() = 
        cell.Next()
        dungeonsAndBoxesLastChangedTime.SetNow()
        changed.Trigger()
    member _this.CellCurrent() = cell.Current()
    member _this.Set(v,ph) = 
        cell.Set(v)
        playerHas <- ph
        dungeonsAndBoxesLastChangedTime.SetNow()
        changed.Trigger()
    member _this.AttemptToSet(v,ph) = 
        if cell.AttemptToSet(v) then
            playerHas <- ph
            dungeonsAndBoxesLastChangedTime.SetNow()
            changed.Trigger()
            true
        else
            false
    member _this.SetPlayerHas(v) = 
        playerHas <- v
        dungeonsAndBoxesLastChangedTime.SetNow()
        changed.Trigger()

let ladderBox = Box()
let armosBox  = Box()
let sword2Box = Box()

[<RequireQualifiedAccess>]
type DungeonTrackerInstanceKind =
    | HIDE_DUNGEON_NUMBERS
    | DEFAULT

type DungeonTrackerInstance(kind) =
    static let mutable theInstance = None
    let finalBoxOf1Or4 = new Box()  // only relevant in DEFAULT
    let dungeons = 
        match kind with
        | DungeonTrackerInstanceKind.HIDE_DUNGEON_NUMBERS -> [| 
            for i = 0 to 7 do 
                yield new Dungeon(i,3) 
            yield new Dungeon(8,2)
            |]
        | DungeonTrackerInstanceKind.DEFAULT -> [|
            new Dungeon(0, 2)
            new Dungeon(1, 2)
            new Dungeon(2, 2)
            new Dungeon(3, 2)
            new Dungeon(4, 2)
            new Dungeon(5, 2)
            new Dungeon(6, 2)
            new Dungeon(7, 3)
            new Dungeon(8, 2)
            |]
    member _this.Kind = kind
    member _this.Dungeons(i) = dungeons.[i]
    member _this.FinalBoxOf1Or4 =
        match kind with
        | DungeonTrackerInstanceKind.HIDE_DUNGEON_NUMBERS -> failwith "FinalBoxOf1Or4 does not exist in HIDE_DUNGEON_NUMBERS"
        | DungeonTrackerInstanceKind.DEFAULT -> finalBoxOf1Or4
    member _this.AllBoxes() =
        [|
        for d in dungeons do
            yield! d.Boxes
        yield ladderBox
        yield armosBox
        yield sword2Box
        |]
    static member TheDungeonTrackerInstance 
        with get() = 
            match theInstance with | Some i -> i | _ -> failwith "uninitialized TheDungeonTrackerInstance" 
        and set(x:DungeonTrackerInstance) = theInstance <- Some(x)

and Dungeon(id,numBoxes) =
    let mutable playerHasTriforce = false                     // just ignore this for dungeon 9 (id=8)
    let boxes = Array.init numBoxes (fun _ -> new Box())
    let mutable color = 0                // 0xRRGGBB format   // just ignore this for dungeon 9 (id=8)
    let mutable labelChar = '?'          // ?12345678         // just ignore this for dungeon 9 (id=8)
    let hiddenDungeonColorLabelChangeEvent = new Event<_>()
    let playerHasTriforceChangeEvent = new Event<_>()
    let hasBeenLocatedChangeEvent = new Event<_>()
    do
        mapSquareChoiceDomain.Changed.Add(fun (_,key) -> if key=id then hasBeenLocatedChangeEvent.Trigger())
    member _this.HasBeenLocated() = mapSquareChoiceDomain.NumUses(id) = 1   // WARNING: this being true does NOT mean TrackerModel.mapStateSummary.DungeonLocations.[i] has a legal (non-NOTFOUND) value
    member _this.HasBeenLocatedChanged = hasBeenLocatedChangeEvent.Publish
    member _this.PlayerHasTriforce() = playerHasTriforce
    member _this.ToggleTriforce() = playerHasTriforce <- not playerHasTriforce; playerHasTriforceChangeEvent.Trigger(playerHasTriforce); dungeonsAndBoxesLastChangedTime.SetNow()
    member _this.PlayerHasTriforceChanged = playerHasTriforceChangeEvent.Publish
    member _this.Boxes = 
        match DungeonTrackerInstance.TheDungeonTrackerInstance.Kind with
        | DungeonTrackerInstanceKind.HIDE_DUNGEON_NUMBERS -> boxes
        | DungeonTrackerInstanceKind.DEFAULT ->
            if id=0 && not(Options.IsSecondQuestDungeons.Value) || id=3 && Options.IsSecondQuestDungeons.Value then
                [| yield! boxes; yield DungeonTrackerInstance.TheDungeonTrackerInstance.FinalBoxOf1Or4 |]
            else
                boxes
    member this.IsComplete = 
        match DungeonTrackerInstance.TheDungeonTrackerInstance.Kind with
        | DungeonTrackerInstanceKind.HIDE_DUNGEON_NUMBERS ->
            if playerHasTriforce then
                let mutable numBoxesDone = 0
                for b in boxes do
                    if b.PlayerHas() <> PlayerHas.NO then
                        numBoxesDone <- numBoxesDone + 1
                let twoBoxers = if Options.IsSecondQuestDungeons.Value then "123567" else "234567"
                numBoxesDone = 3 || (numBoxesDone = 2 && (twoBoxers |> Seq.contains this.LabelChar))
            else
                false
        | DungeonTrackerInstanceKind.DEFAULT ->
            playerHasTriforce && this.Boxes |> Array.forall (fun b -> b.PlayerHas() <> PlayerHas.NO)
    // for Hidden Dungeon Numbers
    member _this.Color with get() = color and set(x) = color <- x; hiddenDungeonColorLabelChangeEvent.Trigger(color,labelChar)
    member _this.LabelChar with get() = labelChar and set(x) = labelChar <- x; hiddenDungeonColorLabelChangeEvent.Trigger(color,labelChar); dungeonsAndBoxesLastChangedTime.SetNow()
    member _this.HiddenDungeonColorOrLabelChanged = hiddenDungeonColorLabelChangeEvent.Publish

let GetDungeon(i) = DungeonTrackerInstance.TheDungeonTrackerInstance.Dungeons(i)
let IsHiddenDungeonNumbers() = DungeonTrackerInstance.TheDungeonTrackerInstance.Kind = DungeonTrackerInstanceKind.HIDE_DUNGEON_NUMBERS
let GetTriforceHaves() =
    if IsHiddenDungeonNumbers() then
        let haves = Array.zeroCreate 8
        for i = 0 to 7 do
            let d = GetDungeon(i)
            if d.PlayerHasTriforce() then
                if d.LabelChar >= '1' && d.LabelChar <= '8' then
                    let n = int d.LabelChar - int '1'
                    haves.[n] <- true
        haves
    else
        [| for i = 0 to 7 do yield GetDungeon(i).PlayerHasTriforce() |]

//////////////////////////////////////////////////////////////////////////////////////////
// Player computed state summary

type PlayerComputedStateSummary(haveRecorder,haveLadder,haveAnyKey,haveCoastItem,haveWhiteSwordItem,havePowerBracelet,haveRaft,playerHearts,
                                swordLevel,candleLevel,ringLevel,haveBow,arrowLevel,haveWand,haveBook,boomerangLevel) = 
    // computed from Boxes and other bits
    member _this.HaveRecorder = haveRecorder
    member _this.HaveLadder = haveLadder
    member _this.HaveAnyKey = haveAnyKey
    member _this.HaveCoastItem = haveCoastItem
    member _this.HaveWhiteSwordItem = haveWhiteSwordItem
    member _this.HavePowerBracelet = havePowerBracelet
    member _this.HaveRaft = haveRaft
    member _this.PlayerHearts = playerHearts // TODO can't handle money-or-life rooms losing heart, or flags that start with more hearts
    member _this.SwordLevel = swordLevel
    member _this.CandleLevel = candleLevel
    member _this.RingLevel = ringLevel
    member _this.HaveBow = haveBow
    member _this.ArrowLevel = arrowLevel
    member _this.HaveWand = haveWand
    member _this.HaveBookOrShield = haveBook
    member _this.BoomerangLevel = boomerangLevel
let mutable playerComputedStateSummary = PlayerComputedStateSummary(false,false,false,false,false,false,false,3,0,0,0,false,0,false,false,0)
let playerComputedStateSummaryLastComputedTime = new LastChangedTime()
let recomputePlayerStateSummary() =
    let mutable haveRecorder,haveLadder,haveAnyKey,haveCoastItem,haveWhiteSwordItem,havePowerBracelet,haveRaft,playerHearts = false,false,false,false,false,false,false,3
    let mutable swordLevel,candleLevel,ringLevel,haveBow,arrowLevel,haveWand,haveBookOrShield,boomerangLevel = 0,0,0,false,0,false,false,0
    for b in DungeonTrackerInstance.TheDungeonTrackerInstance.AllBoxes() do
        if b.PlayerHas() = PlayerHas.YES then
            if b.CellCurrent() = ITEMS.RECORDER then
                haveRecorder <- true
            elif b.CellCurrent() = ITEMS.LADDER then
                haveLadder <- true
            elif b.CellCurrent() = ITEMS.KEY then
                haveAnyKey <- true
            elif b.CellCurrent() = ITEMS.POWERBRACELET then
                havePowerBracelet <- true
            elif b.CellCurrent() = ITEMS.RAFT then
                haveRaft <- true
            elif b.CellCurrent() = ITEMS.REDCANDLE then
                candleLevel <- 2
            elif b.CellCurrent() = ITEMS.HEARTCONTAINER then
                playerHearts <- playerHearts + 1
            elif b.CellCurrent() = ITEMS.BOOKSHIELD then
                haveBookOrShield <- true
            elif b.CellCurrent() = ITEMS.BOOMERANG then
                boomerangLevel <- max boomerangLevel 1
            elif b.CellCurrent() = ITEMS.BOW then
                haveBow <- true
            elif b.CellCurrent() = ITEMS.MAGICBOOMERANG then
                boomerangLevel <- 2
            elif b.CellCurrent() = ITEMS.REDRING then
                ringLevel <- 2
            elif b.CellCurrent() = ITEMS.SILVERARROW then
                arrowLevel <- 2
            elif b.CellCurrent() = ITEMS.WAND then
                haveWand <- true
            elif b.CellCurrent() = ITEMS.WHITESWORD then
                swordLevel <- max swordLevel 2
    if playerProgressAndTakeAnyHearts.PlayerHasBlueCandle.Value() then
        candleLevel <- max candleLevel 1
    if playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Value() then
        swordLevel <- 3
    if playerProgressAndTakeAnyHearts.PlayerHasWoodSword.Value() then
        swordLevel <- max swordLevel 1
    if playerProgressAndTakeAnyHearts.PlayerHasBlueRing.Value() then
        ringLevel <- max ringLevel 1
    if playerProgressAndTakeAnyHearts.PlayerHasWoodArrow.Value() then
        arrowLevel <- max arrowLevel 1
    if ladderBox.PlayerHas() <> PlayerHas.NO then
        haveCoastItem <- true
    if sword2Box.PlayerHas() <> PlayerHas.NO then
        haveWhiteSwordItem <- true
    for h = 0 to 3 do
        if playerProgressAndTakeAnyHearts.GetTakeAnyHeart(h) = 1 then
            playerHearts <- playerHearts + 1
    playerComputedStateSummary <- PlayerComputedStateSummary(haveRecorder,haveLadder,haveAnyKey,haveCoastItem,haveWhiteSwordItem,havePowerBracelet,haveRaft,playerHearts,
                                                                swordLevel,candleLevel,ringLevel,haveBow,arrowLevel,haveWand,haveBookOrShield,boomerangLevel)
    playerComputedStateSummaryLastComputedTime.SetNow()

//////////////////////////////////////////////////////////////////////////////////////////
// Map

let mutable owInstance = new OverworldData.OverworldInstance(OverworldData.FIRST)

let mapLastChangedTime = new LastChangedTime()
let mutable overworldMapMarks : Cell[,] = null
let private overworldMapExtraData = Array2D.init 16 8 (fun _ _ -> Array.zeroCreate MapSquareChoiceDomainHelper.DARK_X)
// extra data key-value store, used by 
//  - 3-item shops to store the second item, key for all shops is SHOP, value 0 is none and 1-MapStateProxy.NUM_ITEMS are those items
//  - various others store a brightness toggle, key is <mapstate>, value is 0 or <mapstate>
let getOverworldMapExtraData(i,j,k) = 
#if DEBUG
    let cur = overworldMapMarks.[i,j].Current()
    if cur=k || (MapSquareChoiceDomainHelper.IsItem(cur) && k=MapSquareChoiceDomainHelper.SHOP) then
        () // ok
    else
        printfn "dodgy, but there are legal times to be here, e.g. popup redrawing-on-hover when changing from non-shop to shop"  // put a breakpoint here for debugging
#endif
    overworldMapExtraData.[i,j].[k]
let setOverworldMapExtraData(i,j,k,v) = 
    overworldMapExtraData.[i,j].[k] <- v
    mapLastChangedTime.SetNow()
let NOTFOUND = (-1,-1)
type MapStateSummary(dungeonLocations,anyRoadLocations,armosLocation,sword3Location,sword2Location,sword1Location,boomBookShopLocation,owSpotsRemain,owGettableLocations,
                        owWhistleSpotsRemain,owPowerBraceletSpotsRemain,owRouteworthySpots,firstQuestOnlyInterestingMarks,secondQuestOnlyInterestingMarks) =
    member _this.DungeonLocations = dungeonLocations
    member _this.AnyRoadLocations = anyRoadLocations
    member _this.ArmosLocation = armosLocation
    member _this.Sword3Location = sword3Location
    member _this.Sword2Location = sword2Location
    member _this.Sword1Location = sword1Location
    member _this.BoomBookShopLocation = boomBookShopLocation
    member _this.OwSpotsRemain = owSpotsRemain
    member _this.OwGettableLocations = owGettableLocations
    member _this.OwWhistleSpotsRemain = owWhistleSpotsRemain
    member _this.OwPowerBraceletSpotsRemain = owPowerBraceletSpotsRemain
    member _this.OwRouteworthySpots = owRouteworthySpots
    member _this.FirstQuestOnlyInterestingMarks = firstQuestOnlyInterestingMarks
    member _this.SecondQuestOnlyInterestingMarks = secondQuestOnlyInterestingMarks
let mutable mapStateSummary = MapStateSummary(null,null,NOTFOUND,NOTFOUND,NOTFOUND,NOTFOUND,NOTFOUND,0,ResizeArray(),null,0,null,null,null)
let mapStateSummaryComputedEvent = new Event<_>()
let mapStateSummaryLastComputedTime = new LastChangedTime()
let recomputeMapStateSummary() =
    let dungeonLocations = Array.create 9 NOTFOUND
    let anyRoadLocations = Array.create 4 NOTFOUND
    let mutable armosLocation = NOTFOUND
    let mutable sword3Location = NOTFOUND
    let mutable sword2Location = NOTFOUND
    let mutable sword1Location = NOTFOUND
    let mutable boomBookShopLocation = NOTFOUND  // i think there can be at most one?
    let mutable owSpotsRemain = 0
    let owGettableLocations = ResizeArray()
    let owWhistleSpotsRemain = ResizeArray()
    let mutable owPowerBraceletSpotsRemain = 0
    let mutable owRouteworthySpots = Array2D.create 16 8 false
    let firstQuestOnlyInterestingMarks = Array2D.zeroCreate 16 8
    let secondQuestOnlyInterestingMarks = Array2D.zeroCreate 16 8
    for i = 0 to 15 do
        for j = 0 to 7 do
            if not(owInstance.AlwaysEmpty(i,j)) then
                match overworldMapMarks.[i,j].Current() with
                | x when x>=0 && x<9 -> 
                    dungeonLocations.[x] <- i,j
                    if x < 8 && not(GetDungeon(x).IsComplete) then
                        owRouteworthySpots.[i,j] <- true
                    let mutable playerHasAllTriforce = true
                    for i = 0 to 7 do
                        if not(GetDungeon(i).PlayerHasTriforce()) then
                            playerHasAllTriforce <- false
                    if x = 8 && playerHasAllTriforce then
                        owRouteworthySpots.[i,j] <- true
                | x when x>=9 && x<13 -> 
                    anyRoadLocations.[x-9] <- i,j
                | x when x=MapSquareChoiceDomainHelper.SWORD3 -> 
                    sword3Location <- i,j
                    if not(playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Value()) && playerComputedStateSummary.PlayerHearts >= 10 then
                        owRouteworthySpots.[i,j] <- true
                | x when x=MapSquareChoiceDomainHelper.SWORD2 -> 
                    sword2Location <- i,j
                    if not playerComputedStateSummary.HaveWhiteSwordItem && playerComputedStateSummary.PlayerHearts >= 4 then
                        owRouteworthySpots.[i,j] <- true
                | x when x=MapSquareChoiceDomainHelper.SWORD1 -> 
                    sword1Location <- i,j
                | n when n=MapSquareChoiceDomainHelper.ARMOS -> 
                    armosLocation <- i,j
                    if armosBox.PlayerHas() = PlayerHas.NO then
                        owRouteworthySpots.[i,j] <- true
                | -1 ->
                    owSpotsRemain <- owSpotsRemain + 1
                    if owInstance.Whistleable(i,j) then
                        owWhistleSpotsRemain.Add(i,j)
                    if owInstance.PowerBraceletable(i,j) then
                        owPowerBraceletSpotsRemain <- owPowerBraceletSpotsRemain + 1
                    if (owInstance.Whistleable(i,j) && not playerComputedStateSummary.HaveRecorder) ||
                        (owInstance.PowerBraceletable(i,j) && not playerComputedStateSummary.HavePowerBracelet) ||
                        (owInstance.Ladderable(i,j) && not playerComputedStateSummary.HaveLadder) ||
                        (owInstance.Raftable(i,j) && not playerComputedStateSummary.HaveRaft) ||
                        (owInstance.Bombable(i,j) && not(playerProgressAndTakeAnyHearts.PlayerHasBombs.Value())) ||
                        (owInstance.Burnable(i,j) && playerComputedStateSummary.CandleLevel=0) then
                        ()
                    else
                        owRouteworthySpots.[i,j] <- true
                        owGettableLocations.Add(i,j)
                | _ -> () // shop or whatnot
                let cur = overworldMapMarks.[i,j].Current()
                if MapSquareChoiceDomainHelper.IsItem(cur) then
                    if cur = MapSquareChoiceDomainHelper.BOOK || 
                            (getOverworldMapExtraData(i,j,MapSquareChoiceDomainHelper.SHOP) = MapSquareChoiceDomainHelper.ToItem(MapSquareChoiceDomainHelper.BOOK)) then
                        boomBookShopLocation <- i,j
                let isInteresting = overworldMapMarks.[i,j].Current() <> -1 && overworldMapMarks.[i,j].Current() <> mapSquareChoiceDomain.MaxKey
                if OverworldData.owMapSquaresSecondQuestOnly.[j].Chars(i) = 'X' then 
                    secondQuestOnlyInterestingMarks.[i,j] <- isInteresting 
                if OverworldData.owMapSquaresFirstQuestOnly.[j].Chars(i) = 'X' then 
                    firstQuestOnlyInterestingMarks.[i,j] <- isInteresting 
    owRouteworthySpots.[15,5] <- playerComputedStateSummary.HaveLadder && not playerComputedStateSummary.HaveCoastItem // gettable coast item is routeworthy
    mapStateSummary <- MapStateSummary(dungeonLocations,anyRoadLocations,armosLocation,sword3Location,sword2Location,sword1Location,boomBookShopLocation,owSpotsRemain,owGettableLocations,
                                        owWhistleSpotsRemain,owPowerBraceletSpotsRemain,owRouteworthySpots,firstQuestOnlyInterestingMarks,secondQuestOnlyInterestingMarks)
    mapStateSummaryLastComputedTime.SetNow()
    mapStateSummaryComputedEvent.Trigger()

//////////////////////////////////////////////////////////////////////////////////////////
// Dungeon blockers

// only some of these can participate in semantic reminders, obviously...
[<RequireQualifiedAccess>]
type DungeonBlocker =
    | COMBAT    // need better weapon/armor
    | BOW_AND_ARROW
    | RECORDER
    | LADDER  
    | BAIT    
    | KEY
    | BOMB
//    | MONEY     // money or life room? bomb upgrade?
    | NOTHING
    member this.DisplayDescription() =
        match this with
        | DungeonBlocker.COMBAT -> "Need better\nweapon/armor"
        | DungeonBlocker.BOW_AND_ARROW -> "Need bow&arrow"
        | DungeonBlocker.RECORDER -> "Need recorder"
        | DungeonBlocker.LADDER -> "Need ladder"
        | DungeonBlocker.BAIT -> "Need meat"
        | DungeonBlocker.KEY -> "Need keys"
        | DungeonBlocker.BOMB -> "Need bombs"
        | DungeonBlocker.NOTHING -> "Unmarked"
    member this.Next() =
        match this with
        | DungeonBlocker.COMBAT -> DungeonBlocker.BOW_AND_ARROW
        | DungeonBlocker.BOW_AND_ARROW -> DungeonBlocker.RECORDER
        | DungeonBlocker.RECORDER -> DungeonBlocker.LADDER
        | DungeonBlocker.LADDER -> DungeonBlocker.BAIT
        | DungeonBlocker.BAIT -> DungeonBlocker.KEY
        | DungeonBlocker.KEY -> DungeonBlocker.BOMB
        | DungeonBlocker.BOMB -> DungeonBlocker.NOTHING
        | DungeonBlocker.NOTHING -> DungeonBlocker.COMBAT
    member this.Prev() =
        match this with
        | DungeonBlocker.COMBAT -> DungeonBlocker.NOTHING
        | DungeonBlocker.BOW_AND_ARROW -> DungeonBlocker.COMBAT
        | DungeonBlocker.RECORDER -> DungeonBlocker.BOW_AND_ARROW
        | DungeonBlocker.LADDER -> DungeonBlocker.RECORDER
        | DungeonBlocker.BAIT -> DungeonBlocker.LADDER
        | DungeonBlocker.KEY -> DungeonBlocker.BAIT
        | DungeonBlocker.BOMB -> DungeonBlocker.KEY
        | DungeonBlocker.NOTHING -> DungeonBlocker.BOMB
    static member All = [| 
        DungeonBlocker.COMBAT
        DungeonBlocker.BOW_AND_ARROW
        DungeonBlocker.RECORDER
        DungeonBlocker.LADDER
        DungeonBlocker.BAIT
        DungeonBlocker.KEY
        DungeonBlocker.BOMB
        DungeonBlocker.NOTHING
        |]
[<RequireQualifiedAccess>]
type CombatUnblockerDetail =
    | BETTER_SWORD
    | BETTER_ARMOR
    | WAND
let MAX_BLOCKERS_PER_DUNGEON = 2
let dungeonBlockers = Array2D.create 8 MAX_BLOCKERS_PER_DUNGEON DungeonBlocker.NOTHING  // Note: we don't need to LastComputedTime-invalidate anything when the blocker set changes

//////////////////////////////////////////////////////////////////////////////////////////

let recomputeWhatIsNeeded() =
    let mutable changed = false
    if playerProgressLastChangedTime.Time > playerComputedStateSummaryLastComputedTime.Time ||
        dungeonsAndBoxesLastChangedTime.Time > playerComputedStateSummaryLastComputedTime.Time then
        recomputePlayerStateSummary()
        changed <- true
    if playerComputedStateSummaryLastComputedTime.Time > mapStateSummaryLastComputedTime.Time ||
        mapLastChangedTime.Time > mapStateSummaryLastComputedTime.Time then
        recomputeMapStateSummary()
        changed <- true
    changed

//////////////////////////////////////////////////////////////////////////////////////////
// Other minor bits
    
let mutable shieldBook = false // if true, boomstick seed - no eventing here, UI should synchronously swap shield/book icons
let mutable startIconX,startIconY = NOTFOUND  // UI can poke and display these

[<RequireQualifiedAccess>]
type HintZone =
    | UNKNOWN
    | DEATH_MOUNTAIN
    | LAKE
    | LOST_HILLS
    | RIVER
    | GRAVE
    | DESERT
    | COAST
    | DEAD_WOODS
    | NEAR_START
    | FOREST
    member this.AsDataChar() =  // as per OverworldData.owMapZone
        match this with
        | UNKNOWN -> '_'
        | DEATH_MOUNTAIN -> 'M'
        | LAKE -> 'L'
        | LOST_HILLS -> 'H'
        | RIVER -> 'R'
        | GRAVE -> 'G'
        | DESERT -> 'D'
        | COAST -> 'C'
        | DEAD_WOODS -> 'W'
        | NEAR_START -> 'S'
        | FOREST -> 'F'
    override this.ToString() =
        match this with
        | UNKNOWN -> "?????"
        | DEATH_MOUNTAIN -> "Death Mountain"
        | LAKE -> "Lake"
        | LOST_HILLS -> "Lost Hills"
        | RIVER -> "River"
        | GRAVE -> "Grave"
        | DESERT -> "Desert"
        | COAST -> "Coast"
        | DEAD_WOODS -> "Dead Woods"
        | NEAR_START -> "Close to Start"
        | FOREST -> "Forest"
    static member FromIndex(i) =
        match i with
        | 0 -> UNKNOWN
        | 1 -> DEATH_MOUNTAIN
        | 2 -> LAKE
        | 3 -> LOST_HILLS
        | 4 -> RIVER
        | 5 -> GRAVE
        | 6 -> DESERT
        | 7 -> COAST
        | 8 -> DEAD_WOODS
        | 9 -> NEAR_START
        | 10 -> FOREST
        | _ -> failwith "bad HintZone index"
    member this.ToIndex() =
        match this with
        | UNKNOWN -> 0
        | DEATH_MOUNTAIN -> 1
        | LAKE -> 2
        | LOST_HILLS -> 3
        | RIVER -> 4
        | GRAVE -> 5
        | DESERT -> 6
        | COAST -> 7
        | DEAD_WOODS -> 8
        | NEAR_START -> 9
        | FOREST -> 10
let GetLevelHint, SetLevelHint, LevelHintChanged = 
    let levelHints = Array.create 11 HintZone.UNKNOWN   // 0-8 is L1-9, 9 is WS, 10 is MS
    let levelHintChangeEvents = Array.init 11 (fun _ -> new Event<_>())
    let GetLevelHint(i) = levelHints.[i]
    let SetLevelHint(i,v) = levelHints.[i] <- v; levelHintChangeEvents.[i].Trigger(v)
    let LevelHintChanged(i) = levelHintChangeEvents.[i].Publish
    GetLevelHint, SetLevelHint, LevelHintChanged

let forceUpdate() = 
    // UI can force an update for a few bits that we don't model well yet
    // TODO ideally dont want this, feels like kludge?
    mapLastChangedTime.SetNow()
                
//////////////////////////////////////////////////////////////////////////////////////////

let unreachablePossibleDungeonSpotCount() =
    let mutable count = 0
    for x = 0 to 15 do
        for y = 0 to 7 do
            let cur = overworldMapMarks.[x,y].Current()
            if cur < 9 then  // it's marked as a dungeon, or it's unmarked so it might be an unfound dungeon
                if owInstance.Bombable(x,y) && not(playerProgressAndTakeAnyHearts.PlayerHasBombs.Value()) then
                    count <- count + 1
                if owInstance.Burnable(x,y) && not(playerComputedStateSummary.CandleLevel>0) then
                    count <- count + 1
                if owInstance.Raftable(x,y) && not(playerComputedStateSummary.HaveRaft) then
                    count <- count + 1
                if owInstance.Ladderable(x,y) && not(playerComputedStateSummary.HaveLadder) then
                    count <- count + 1
                if owInstance.Whistleable(x,y) && not(playerComputedStateSummary.HaveRecorder) then
                    count <- count + 1
    count
type TriforceAndGoSummary() =
    // Note: we're just going to assume the player has a sword (or wand in swordless), let's not go nuts with advanced flags and/or unlikely no-sword-but-all-items situations
    let haveBow = playerComputedStateSummary.HaveBow
    let haveSilvers = playerComputedStateSummary.ArrowLevel=2
    let haveLadder = playerComputedStateSummary.HaveLadder
    let haveRecorder = playerComputedStateSummary.HaveLadder
    let unreachableCount = unreachablePossibleDungeonSpotCount()
    let mutable missingTriforceFromLocatedDungeonCount = 0   // TODO advanced flags: only need N triforces to enter 9
    let mutable missingDungeonCount = 0
    let tagLevel = 
        for i = 0 to 8 do
            if not(GetDungeon(i).PlayerHasTriforce()) then
                if not(GetDungeon(i).HasBeenLocated()) then
                    missingDungeonCount <- missingDungeonCount + 1
                elif i <> 8 then // L9 has no triforce
                    missingTriforceFromLocatedDungeonCount <- missingTriforceFromLocatedDungeonCount + 1
        let compute() =
            let mutable score = 100
            let missingPenalty = 15 + unreachableCount
            score <- score - missingDungeonCount*missingPenalty                  // big penalty for unlocated dungeon
            score <- score - missingTriforceFromLocatedDungeonCount*8            // smallish penalty for missing triforce in a dungeon you already located
            if not haveBow then score <- score - 35                              // huge penalty for missing bow
            if not haveSilvers then score <- score - 30                          // huge penalty for missing silvers
            if not haveLadder then score <- score - 15                           // medium penalty for missing ladder
            if not haveRecorder then score <- score - 5                          // small penalty for missing recorder
            //printfn "score: %d" score
            if score < 0 then 0 else score
        // you might need e.g. power bracelet or raft to find missing dungeon, so never TAG without being able to locate them all  
        if missingDungeonCount=0 || unreachableCount=0 then 
            if haveBow && haveSilvers && haveLadder && haveRecorder then
                103
            elif haveBow && haveSilvers && haveLadder then
                102
            elif haveBow && haveSilvers then
                101
            else
                compute()
        else
            compute()
    member _this.Level = tagLevel  // 103 TAG, 102 probably-TAG, 101 might-be-TAG, 1-100 see features below, 0 not worth reporting
    member _this.HaveBow = haveBow
    member _this.HaveSilvers = haveSilvers
    member _this.HaveLadder = haveLadder
    member _this.HaveRecorder = haveRecorder
    member _this.MissingDungeonCount = missingDungeonCount
type ITrackerEvents =
    // hearts
    abstract CurrentHearts : int -> unit
    abstract AnnounceConsiderSword2 : unit -> unit
    abstract AnnounceConsiderSword3 : unit -> unit
    // map
    abstract OverworldSpotsRemaining : int * int -> unit  // total remaining, currently gettable
    abstract DungeonLocation : int*int*int*bool*bool -> unit   // number 0-7, x, y, hasTri, isCompleted --- to update triforce color (found), completed shading, recorderable/completed map icon
    abstract AnyRoadLocation : int*int*int -> unit // number 0-3, x, y
    abstract WhistleableLocation : int*int -> unit // x, y
    abstract Armos : int*int -> unit // x,y
    abstract Sword3 : int*int -> unit // x,y
    abstract Sword2 : int*int -> unit // x,y
    abstract RoutingInfo : bool*bool*seq<int*int>*seq<int*int>*bool[,] -> unit // haveLadder haveRaft currentRecorderWarpDestinations currentAnyRoadDestinations owRouteworthySpots
    // dungeons
    abstract AnnounceCompletedDungeon : int -> unit
    abstract CompletedDungeons : bool[] -> unit     // for current shading
    abstract AnnounceFoundDungeonCount : int -> unit
    abstract AnnounceTriforceCount : int -> unit
    abstract AnnounceTriforceAndGo : int * TriforceAndGoSummary -> unit
    // blockers
    abstract RemindUnblock : DungeonBlocker * seq<int> * seq<CombatUnblockerDetail> -> unit
    // items
    abstract RemindShortly : int -> unit

// state-transition announcements
let haveAnnouncedHearts = Array.zeroCreate 17
let haveAnnouncedCompletedDungeons = Array.zeroCreate 8
let mutable previouslyAnnouncedFoundDungeonCount = 0
let mutable previouslyAnnouncedTriforceCount = 0
let mutable previouslyLocatedDungeonCount = 0
let mutable remindedLadder, remindedAnyKey = false, false
let mutable priorSwordWandLevel = 0
let mutable priorRingLevel = 0
let mutable priorBombs = false
let mutable priorBowArrow = false
let mutable priorRecorder = false
let mutable priorLadder = false
let mutable priorAnyKey = false
// triforce-and-go levels
let mutable previouslyAnnouncedTriforceAndGo = 0  // 0 = no, 1 = might be, 2 = probably, 3 = certainly triforce-and-go
let mutable previousCompletedDungeonCount = 0
let allUIEventingLogic(ite : ITrackerEvents) =
    // hearts
    let playerHearts = playerComputedStateSummary.PlayerHearts
    ite.CurrentHearts playerHearts
    if playerHearts >=4 && playerHearts <= 6 && not haveAnnouncedHearts.[playerHearts] then
        haveAnnouncedHearts.[playerHearts] <- true
        if not playerComputedStateSummary.HaveWhiteSwordItem && mapStateSummary.Sword2Location<>NOTFOUND then
            ite.AnnounceConsiderSword2()
    if playerHearts >=10 && playerHearts <= 14 && not haveAnnouncedHearts.[playerHearts] then
        haveAnnouncedHearts.[playerHearts] <- true
        if not(playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Value()) && mapStateSummary.Sword3Location<>NOTFOUND then
            ite.AnnounceConsiderSword3()
    // map
    ite.OverworldSpotsRemaining(mapStateSummary.OwSpotsRemain, mapStateSummary.OwGettableLocations.Count)
    for d = 0 to 8 do
        if mapStateSummary.DungeonLocations.[d] <> NOTFOUND then
            let x,y = mapStateSummary.DungeonLocations.[d]
            ite.DungeonLocation(d, x, y, GetDungeon(d).PlayerHasTriforce(), GetDungeon(d).IsComplete)
    for a = 0 to 3 do
        if mapStateSummary.AnyRoadLocations.[a] <> NOTFOUND then
            let x,y = mapStateSummary.AnyRoadLocations.[a]
            ite.AnyRoadLocation(a, x, y)
    for x,y in mapStateSummary.OwWhistleSpotsRemain do
        ite.WhistleableLocation(x, y)
    if mapStateSummary.ArmosLocation <> NOTFOUND then
        ite.Armos(mapStateSummary.ArmosLocation)
    if mapStateSummary.Sword3Location <> NOTFOUND then
        ite.Sword3(mapStateSummary.Sword3Location)
    if mapStateSummary.Sword2Location <> NOTFOUND then
        ite.Sword2(mapStateSummary.Sword2Location)
    let recorderDests = [|
        if playerComputedStateSummary.HaveRecorder then
            for d = 0 to 7 do
                if mapStateSummary.DungeonLocations.[d] <> NOTFOUND && GetDungeon(d).PlayerHasTriforce() then
                    yield mapStateSummary.DungeonLocations.[d]
        |]
    let anyRoadDests = [|
        for d in mapStateSummary.AnyRoadLocations do
            if d <> NOTFOUND then
                yield d
        |]
    ite.RoutingInfo(playerComputedStateSummary.HaveLadder, playerComputedStateSummary.HaveRaft, recorderDests, anyRoadDests, mapStateSummary.OwRouteworthySpots)
    // dungeons
    for d = 0 to 7 do
        if not haveAnnouncedCompletedDungeons.[d] && GetDungeon(d).IsComplete then
            ite.AnnounceCompletedDungeon(d)
            haveAnnouncedCompletedDungeons.[d] <- true
    ite.CompletedDungeons [| for d = 0 to 7 do yield GetDungeon(d).IsComplete |]
    let mutable numFound = 0
    for d = 0 to 8 do
        if mapStateSummary.DungeonLocations.[d]<>NOTFOUND then
            numFound <- numFound + 1
    if numFound > previouslyAnnouncedFoundDungeonCount then
        ite.AnnounceFoundDungeonCount(numFound)
        previouslyAnnouncedFoundDungeonCount <- numFound
    let mutable triforces = 0
    for d = 0 to 8 do
        if GetDungeon(d).PlayerHasTriforce() then
            triforces <- triforces + 1
    let mutable locatedDungeons = 0
    let mutable completedDungeons = 0
    for d = 0 to 8 do
        if GetDungeon(d).IsComplete then
            completedDungeons <- completedDungeons + 1
        if GetDungeon(d).HasBeenLocated() then
            locatedDungeons <- locatedDungeons + 1
    let tagSummary = new TriforceAndGoSummary()
    let mutable justAnnouncedTAG = false
    if triforces > previouslyAnnouncedTriforceCount then
        ite.AnnounceTriforceCount(triforces)
        previouslyAnnouncedTriforceCount <- triforces
        if completedDungeons <= previousCompletedDungeonCount && tagSummary.Level > 101 then
            // just got a new triforce, it did not complete a dungeon, but the player is probably triforce and go, so remind them, so they might abandon rest of dungeon
            ite.AnnounceTriforceAndGo(triforces, tagSummary)
            justAnnouncedTAG <- true
    if locatedDungeons > previouslyLocatedDungeonCount && tagSummary.Level > 101 && not justAnnouncedTAG then
        // just located a new dungeon, the player is probably triforce and go, so remind them
        ite.AnnounceTriforceAndGo(triforces, tagSummary)
        justAnnouncedTAG <- true
    previousCompletedDungeonCount <- completedDungeons
    previouslyLocatedDungeonCount <- locatedDungeons
    if tagSummary.Level > previouslyAnnouncedTriforceAndGo then
        previouslyAnnouncedTriforceAndGo <- tagSummary.Level
        if not justAnnouncedTAG then
            ite.AnnounceTriforceAndGo(triforces, tagSummary)
    // blockers - COMBAT
    let combatUnblockers = ResizeArray()
    if playerComputedStateSummary.SwordLevel > priorSwordWandLevel then
        combatUnblockers.Add(CombatUnblockerDetail.BETTER_SWORD)
    if playerComputedStateSummary.HaveWand && (priorSwordWandLevel < 2) then
        combatUnblockers.Add(CombatUnblockerDetail.WAND)
    if playerComputedStateSummary.RingLevel > priorRingLevel 
                && (playerComputedStateSummary.SwordLevel>0 || playerComputedStateSummary.HaveWand) then  // armor won't help you win combat if you have 0 weapons
        combatUnblockers.Add(CombatUnblockerDetail.BETTER_ARMOR)
    if combatUnblockers.Count > 0 then
        let dungeonIdxs = ResizeArray()
        for i = 0 to 7 do
            if dungeonBlockers.[i,0] = DungeonBlocker.COMBAT || dungeonBlockers.[i,1] = DungeonBlocker.COMBAT then
                if not(GetDungeon(i).IsComplete) then
                    dungeonIdxs.Add(i)
        if dungeonIdxs.Count > 0 then
            if tagSummary.Level < 103 then // no need for blocker-reminder if fully-go-time
                ite.RemindUnblock(DungeonBlocker.COMBAT, dungeonIdxs, combatUnblockers)
    priorSwordWandLevel <- max playerComputedStateSummary.SwordLevel (if playerComputedStateSummary.HaveWand then 2 else 0)
    priorRingLevel <- playerComputedStateSummary.RingLevel
    // blockers - generic
    let blockerLogic(db) =
        let dungeonIdxs = ResizeArray()
        for i = 0 to 7 do
            if dungeonBlockers.[i,0] = db || dungeonBlockers.[i,1] = db then
                if not(GetDungeon(i).IsComplete) then
                    dungeonIdxs.Add(i)
        if dungeonIdxs.Count > 0 then
            if tagSummary.Level < 103 then // no need for blocker-reminder if fully-go-time
                ite.RemindUnblock(db, dungeonIdxs, [])
    // blockers - others
    if not priorBombs && playerProgressAndTakeAnyHearts.PlayerHasBombs.Value() then
        blockerLogic(DungeonBlocker.BOMB)
    priorBombs <- playerProgressAndTakeAnyHearts.PlayerHasBombs.Value()

    if not priorBowArrow && playerComputedStateSummary.HaveBow && playerComputedStateSummary.ArrowLevel>=1 then
        blockerLogic(DungeonBlocker.BOW_AND_ARROW)
    priorBowArrow <- playerComputedStateSummary.HaveBow && playerComputedStateSummary.ArrowLevel>=1

    if not priorRecorder && playerComputedStateSummary.HaveRecorder then
        blockerLogic(DungeonBlocker.RECORDER)
    priorRecorder <- playerComputedStateSummary.HaveRecorder

    if not priorLadder && playerComputedStateSummary.HaveLadder then
        blockerLogic(DungeonBlocker.LADDER)
    priorLadder <- playerComputedStateSummary.HaveLadder

    if not priorAnyKey && playerComputedStateSummary.HaveAnyKey then
        blockerLogic(DungeonBlocker.KEY)
    priorAnyKey <- playerComputedStateSummary.HaveAnyKey

    // Note: no logic for BAIT or loose KEYs, as the tracker has no reliable knowledge of this aspect of player's inventory

    // items
    if not remindedLadder && playerComputedStateSummary.HaveLadder then
        ite.RemindShortly(ITEMS.LADDER)
        remindedLadder <- true
    if not remindedAnyKey && playerComputedStateSummary.HaveAnyKey then
        ite.RemindShortly(ITEMS.KEY)
        remindedAnyKey <- true

 ///////////////////////////////////////////////////////

let initializeAll(instance:OverworldData.OverworldInstance, kind) =
    if mapSquareChoiceDomain = null then
        mapSquareChoiceDomain <- ChoiceDomain("mapSquare", overworldTiles(instance.Quest.IsFirstQuestOW) |> Array.map (fun (_,x,_) -> x))
        mapSquareChoiceDomain.Changed.Add(fun _ -> mapLastChangedTime.SetNow())
        overworldMapMarks <- Array2D.init 16 8 (fun _ _ -> new Cell(mapSquareChoiceDomain))  
    else
        failwith "cannot initialize mapSquareChoiceDomain twice"

    let dungeonInstance = new DungeonTrackerInstance(kind)

    DungeonTrackerInstance.TheDungeonTrackerInstance <- dungeonInstance
    owInstance <- instance
    for i = 0 to 15 do
        for j = 0 to 7 do
            if owInstance.AlwaysEmpty(i,j) then
                overworldMapMarks.[i,j].Prev()   // set to 'X'
    recomputeMapStateSummary()

        