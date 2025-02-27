﻿module WPFUI

open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

open OverworldMapTileCustomization
open HotKeys.MyKey

module OW_ITEM_GRID_LOCATIONS = OverworldMapTileCustomization.OW_ITEM_GRID_LOCATIONS

let canvasAdd = Graphics.canvasAdd
let voice = OptionsMenu.voice
let makeHintHighlight = Views.makeHintHighlight

let upcb(bmp) : FrameworkElement = upcast Graphics.BMPtoImage bmp
let mutable reminderAgent = MailboxProcessor.Start(fun _ -> async{return ()})
let SendReminder(category, text:string, icons:seq<FrameworkElement>) =
    if not(TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasRescuedZelda.Value()) then  // if won the game, quit sending reminders
        let shouldRemindVoice, shouldRemindVisual =
            match category with
            | TrackerModel.ReminderCategory.Blockers ->        TrackerModel.Options.VoiceReminders.Blockers.Value,        TrackerModel.Options.VisualReminders.Blockers.Value
            | TrackerModel.ReminderCategory.CoastItem ->       TrackerModel.Options.VoiceReminders.CoastItem.Value,       TrackerModel.Options.VisualReminders.CoastItem.Value
            | TrackerModel.ReminderCategory.DungeonFeedback -> TrackerModel.Options.VoiceReminders.DungeonFeedback.Value, TrackerModel.Options.VisualReminders.DungeonFeedback.Value
            | TrackerModel.ReminderCategory.HaveKeyLadder ->   TrackerModel.Options.VoiceReminders.HaveKeyLadder.Value,   TrackerModel.Options.VisualReminders.HaveKeyLadder.Value
            | TrackerModel.ReminderCategory.RecorderPBSpotsAndBoomstickBook -> TrackerModel.Options.VoiceReminders.RecorderPBSpotsAndBoomstickBook.Value, TrackerModel.Options.VisualReminders.RecorderPBSpotsAndBoomstickBook.Value
            | TrackerModel.ReminderCategory.SwordHearts ->     TrackerModel.Options.VoiceReminders.SwordHearts.Value,     TrackerModel.Options.VisualReminders.SwordHearts.Value
        if shouldRemindVoice || shouldRemindVisual then 
            reminderAgent.Post(text, shouldRemindVoice, icons, shouldRemindVisual)
let ReminderTextBox(txt) : FrameworkElement = 
    upcast new TextBox(Text=txt, Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=20., FontWeight=FontWeights.Bold, IsHitTestVisible=false,
        VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), TextAlignment=TextAlignment.Center)

let gridAdd = Graphics.gridAdd
let gridAddTuple(g,e,(x,y)) = gridAdd(g,e,x,y)
let makeGrid = Graphics.makeGrid

let OMTW = OverworldRouteDrawing.OMTW  // overworld map tile width - at normal aspect ratio, is 48 (16*3)
let routeDrawingCanvas = new Canvas(Width=16.*OMTW, Height=float(8*11*3))

let triforceInnerCanvases = Array.zeroCreate 8
let mainTrackerCanvases : Canvas[,] = Array2D.zeroCreate 9 5
let mainTrackerCanvasShaders : Canvas[,] = Array2D.init 8 5 (fun _i j -> new Canvas(Width=30., Height=30., Background=Brushes.Black, Opacity=(if j=1 then 0.4 else 0.3), IsHitTestVisible=false))
let currentMaxHeartsTextBox = new TextBox(Width=100., Height=20., FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text=sprintf "Max Hearts: %d" TrackerModel.playerComputedStateSummary.PlayerHearts)
let owRemainingScreensTextBox = new TextBox(Width=110., Height=20., FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text=sprintf "%d OW spots left" TrackerModel.mapStateSummary.OwSpotsRemain)
let owGettableScreensTextBox = new TextBox(Width=80., Height=20., FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text=sprintf "%d gettable" TrackerModel.mapStateSummary.OwGettableLocations.Count)
let owGettableScreensCheckBox = new CheckBox(Content = owGettableScreensTextBox)
let ensureRespectingOwGettableScreensCheckBox() =
    if owGettableScreensCheckBox.IsChecked.HasValue && owGettableScreensCheckBox.IsChecked.Value then
        OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, TrackerModel.mapStateSummary.OwRouteworthySpots, 
                    TrackerModel.overworldMapMarks |> Array2D.map (fun cell -> cell.Current() = -1), Point(0.,0.), 0, 0, false, true, 128)

type RouteDestination = LinkRouting.RouteDestination

let drawRoutesTo(routeDestinationOption, routeDrawingCanvas, point, i, j, drawRouteMarks, maxYellowGreenHighlights) =
    let maxYellowGreenHighlights = if owGettableScreensCheckBox.IsChecked.HasValue && owGettableScreensCheckBox.IsChecked.Value then 128 else maxYellowGreenHighlights
    let unmarked = TrackerModel.overworldMapMarks |> Array2D.map (fun cell -> cell.Current() = -1)
    let interestingButInaccesible = ResizeArray()
    let owTargetworthySpots = Array2D.zeroCreate 16 8
    let processHint(hz:TrackerModel.HintZone,couldBeLetterDungeon) =
        for i = 0 to 15 do
            for j = 0 to 7 do
                if OverworldData.owMapZone.[j].[i] = hz.AsDataChar() then
                    let cur = TrackerModel.overworldMapMarks.[i,j].Current()
                    let cbld = (couldBeLetterDungeon && cur>=0 && cur<=7 && TrackerModel.GetDungeon(cur).LabelChar='?')
                    if cur = -1 || cbld then
                        if TrackerModel.mapStateSummary.OwGettableLocations.Contains(i,j) || cbld then
                            owTargetworthySpots.[i,j] <- true
                            unmarked.[i,j] <- true  // for cbld case
                        else
                            interestingButInaccesible.Add(i,j)
    match routeDestinationOption with
    | Some(RouteDestination.SHOP(targetItem)) ->
        for x = 0 to 15 do
            for y = 0 to 7 do
                let msp = MapStateProxy(TrackerModel.overworldMapMarks.[x,y].Current())
                if msp.State = targetItem || (msp.IsThreeItemShop && TrackerModel.getOverworldMapExtraData(x,y,TrackerModel.MapSquareChoiceDomainHelper.SHOP) = TrackerModel.MapSquareChoiceDomainHelper.ToItem(targetItem)) then
                    owTargetworthySpots.[x,y] <- true
        OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, owTargetworthySpots, unmarked, point, i, j, true, false, maxYellowGreenHighlights)
    | Some(RouteDestination.OW_MAP(x,y)) ->
        owTargetworthySpots.[x,y] <- true
        OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, owTargetworthySpots, unmarked, point, i, j, true, false, maxYellowGreenHighlights)
    | Some(RouteDestination.HINTZONE(hz,couldBeLetterDungeon)) ->
        processHint(hz,couldBeLetterDungeon)
        OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, owTargetworthySpots, unmarked, point, i, j, true, false, 128)
    | Some(RouteDestination.UNMARKEDINSTANCEFUNC(f)) ->
        for x = 0 to 15 do
            for y = 0 to 7 do
                if unmarked.[x,y] && f(x,y) then
                    owTargetworthySpots.[x,y] <- true
        OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, owTargetworthySpots, unmarked, point, i, j, true, false, 128)
    | None ->
        OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, TrackerModel.mapStateSummary.OwRouteworthySpots, unmarked, point, i, j, drawRouteMarks, true, maxYellowGreenHighlights)
    for i,j in interestingButInaccesible do
        let rect = new Graphics.TileHighlightRectangle()
        rect.MakeRed()
        for s in rect.Shapes do
            Graphics.canvasAdd(routeDrawingCanvas, s, OMTW*float(i), float(j*11*3))


let resetTimerEvent = new Event<unit>()
let mutable currentlyMousedOWX, currentlyMousedOWY = -1, -1
let mutable notesTextBox = null : TextBox
let mutable timeTextBox = null : TextBox
let H = 30
let RIGHT_COL = 440.
let WEBCAM_LINE = OMTW*16.-200.  // height of upper area is 150, so 200 wide is 4x3 box in upper right; timer and other controls here could be obscured
let TCH = 123  // timeline height
let TH = DungeonUI.TH // text height
let resizeMapTileImage = OverworldMapTileCustomization.resizeMapTileImage

[<RequireQualifiedAccess>]
type ShowLocatorDescriptor =
    | DungeonNumber of int   // 0-7 means dungeon 1-8
    | DungeonIndex of int    // 0-8 means 123456789 or ABCDEFGH9 in top-left-ui presentation order
    | Sword1
    | Sword2
    | Sword3
let makeAll(mainWindow:Window, cm:CustomComboBoxes.CanvasManager, owMapNum, heartShuffle, kind, speechRecognitionInstance:SpeechRecognition.SpeechRecognitionInstance) =
    let refocusMainWindow() =   // keep hotkeys working
        async {
            let ctxt = System.Threading.SynchronizationContext.Current
            do! Async.Sleep(500)  // give new window time to pop up
            do! Async.SwitchToContext(ctxt)
            mainWindow.Focus() |> ignore
        } |> Async.StartImmediate
    // initialize based on startup parameters
    let owMapBMPs, isMixed, owInstance =
        match owMapNum with
        | 0 -> Graphics.overworldMapBMPs(0), false, new OverworldData.OverworldInstance(OverworldData.FIRST)
        | 1 -> Graphics.overworldMapBMPs(1), false, new OverworldData.OverworldInstance(OverworldData.SECOND)
        | 2 -> Graphics.overworldMapBMPs(2), true,  new OverworldData.OverworldInstance(OverworldData.MIXED_FIRST)
        | 3 -> Graphics.overworldMapBMPs(3), true,  new OverworldData.OverworldInstance(OverworldData.MIXED_SECOND)
        | _ -> failwith "bad/unsupported owMapNum"
    TrackerModel.initializeAll(owInstance, kind)
    if not heartShuffle then
        for i = 0 to 7 do
            TrackerModel.GetDungeon(i).Boxes.[0].Set(TrackerModel.ITEMS.HEARTCONTAINER, TrackerModel.PlayerHas.NO)

    // make the entire UI
    let timelineItems = ResizeArray()

    let isSpecificRouteTargetActive,currentRouteTarget,eliminateCurrentRouteTarget,changeCurrentRouteTarget =
        let routeTargetLastClickedTime = new TrackerModel.LastChangedTime(TimeSpan.FromMinutes(10.))
        let mutable routeTarget = None
        let isSpecificRouteTargetActive() = DateTime.Now - routeTargetLastClickedTime.Time < TimeSpan.FromSeconds(10.)
        let currentRouteTarget() =
            if isSpecificRouteTargetActive() then
                routeTarget
            else
                None
        let eliminateCurrentRouteTarget() =
            routeTarget <- None
            routeTargetLastClickedTime.SetAgo(TimeSpan.FromMinutes(10.))
        let changeCurrentRouteTarget(newTarget) =
            routeTargetLastClickedTime.SetNow()
            routeTarget <- Some(newTarget)
        isSpecificRouteTargetActive,currentRouteTarget,eliminateCurrentRouteTarget,changeCurrentRouteTarget
    
    let mutable showLocatorExactLocation = fun(_x:int,_y:int) -> ()
    let mutable showLocatorHintedZone = fun(_hz:TrackerModel.HintZone,_also:bool) -> ()
    let mutable showLocatorInstanceFunc = fun(_f:int*int->bool) -> ()
    let mutable showShopLocatorInstanceFunc = fun(_item:int) -> ()
    let mutable showLocator = fun(_sld:ShowLocatorDescriptor) -> ()
    let mutable hideLocator = fun() -> ()

    let doUIUpdateEvent = new Event<unit>()

    let appMainCanvas = cm.AppMainCanvas
    let mainTracker = makeGrid(9, 5, H, H)
    canvasAdd(appMainCanvas, mainTracker, 0., 0.)

    // numbered triforce display - the extra row of triforce in IsHiddenDungeonNumbers
    let updateNumberedTriforceDisplayImpl(c:Canvas,i) =
        let level = i+1
        let levelLabel = char(int '0' + level)
        let mutable index = -1
        for j = 0 to 7 do
            if TrackerModel.GetDungeon(j).LabelChar = levelLabel then
                index <- j
        let mutable found,hasTriforce = false,false
        if index <> -1 then
            found <- TrackerModel.GetDungeon(index).HasBeenLocated()
            hasTriforce <- TrackerModel.GetDungeon(index).PlayerHasTriforce()
        let hasHint = not(found) && TrackerModel.GetLevelHint(i)<>TrackerModel.HintZone.UNKNOWN
        c.Children.Clear()
        if hasHint then
            c.Children.Add(makeHintHighlight(30.)) |> ignore
        if not hasTriforce then
            if not found then
                c.Children.Add(Graphics.BMPtoImage Graphics.emptyUnfoundNumberedTriforce_bmps.[i]) |> ignore
            else
                c.Children.Add(Graphics.BMPtoImage Graphics.emptyFoundNumberedTriforce_bmps.[i]) |> ignore
        else
            c.Children.Add(Graphics.BMPtoImage Graphics.fullNumberedTriforce_bmps.[i]) |> ignore
    let updateNumberedTriforceDisplayIfItExists =
        if TrackerModel.IsHiddenDungeonNumbers() then
            let numberedTriforceCanvases = Array.init 8 (fun _ -> new Canvas(Width=30., Height=30.))
            for i = 0 to 7 do
                let c = numberedTriforceCanvases.[i]
                canvasAdd(appMainCanvas, c, OW_ITEM_GRID_LOCATIONS.OFFSET+30.*float i, 0.)
                c.MouseEnter.Add(fun _ -> showLocator(ShowLocatorDescriptor.DungeonNumber i))
                c.MouseLeave.Add(fun _ -> hideLocator())
            let update() =
                for i = 0 to 7 do
                    updateNumberedTriforceDisplayImpl(numberedTriforceCanvases.[i], i)
            update
        else
            fun () -> ()
    updateNumberedTriforceDisplayIfItExists()
    // triforce
    for i = 0 to 7 do
        if TrackerModel.IsHiddenDungeonNumbers() then
            // triforce dungeon color
            let colorCanvas = new Canvas(Width=28., Height=28., Background=Brushes.Black)
            //mainTrackerCanvases.[i,0] <- colorCanvas
            let colorButton = new Button(Width=30., Height=30., BorderThickness=Thickness(1.), Margin=Thickness(0.), Padding=Thickness(0.), BorderBrush=Brushes.DimGray, Content=colorCanvas)
            let mutable popupIsActive = false
            colorButton.Click.Add(fun _ -> 
                if not popupIsActive && TrackerModel.IsHiddenDungeonNumbers() then
                    popupIsActive <- true
                    let pos = colorButton.TranslatePoint(Point(15., 15.), appMainCanvas)
                    async {
                        do! Dungeon.HiddenDungeonCustomizerPopup(cm, i, TrackerModel.GetDungeon(i).Color, TrackerModel.GetDungeon(i).LabelChar, false, pos)
                        popupIsActive <- false
                        } |> Async.StartImmediate
                )
            gridAdd(mainTracker, colorButton, i, 0)
            TrackerModel.GetDungeon(i).HiddenDungeonColorOrLabelChanged.Add(fun (color,labelChar) -> 
                colorCanvas.Background <- new SolidColorBrush(Graphics.makeColor(color))
                colorCanvas.Children.Clear()
                let color = if Graphics.isBlackGoodContrast(color) then System.Drawing.Color.Black else System.Drawing.Color.White
                if TrackerModel.GetDungeon(i).LabelChar <> '?' then  // ? and 7 look alike, and also it is easier to parse 'blank' as unknown/unset dungeon number
                    colorCanvas.Children.Add(Graphics.BMPtoImage(Graphics.alphaNumOnTransparentBmp(labelChar, color, 28, 28, 3, 2))) |> ignore
                )
            colorButton.MouseEnter.Add(fun _ -> showLocator(ShowLocatorDescriptor.DungeonIndex i))
            colorButton.MouseLeave.Add(fun _ -> hideLocator())
        else
            let colorCanvas = new Canvas(Width=28., Height=28., Background=Brushes.Black)
            gridAdd(mainTracker, colorCanvas, i, 0)
        // triforce itself and label
        let c = new Canvas(Width=30., Height=30.)
        mainTrackerCanvases.[i,1] <- c
        let innerc = Views.MakeTriforceDisplayView(cm,i,Some(owInstance), true)
        triforceInnerCanvases.[i] <- innerc
        c.Children.Add(innerc) |> ignore
        c.MouseEnter.Add(fun _ -> showLocator(ShowLocatorDescriptor.DungeonIndex i))
        c.MouseLeave.Add(fun _ -> hideLocator())
        gridAdd(mainTracker, c, i, 1)
        let fullTriforceBmp =
            match kind with
            | TrackerModel.DungeonTrackerInstanceKind.HIDE_DUNGEON_NUMBERS -> Graphics.fullLetteredTriforce_bmps.[i]
            | TrackerModel.DungeonTrackerInstanceKind.DEFAULT -> Graphics.fullNumberedTriforce_bmps.[i]
        timelineItems.Add(new Timeline.TimelineItem(fun()->if TrackerModel.GetDungeon(i).PlayerHasTriforce() then Some(fullTriforceBmp) else None))
    let level9ColorCanvas = new Canvas(Width=30., Height=30., Background=Brushes.Black)       // dungeon 9 doesn't need a color, but we don't want to special case nulls
    gridAdd(mainTracker, level9ColorCanvas, 8, 0) 
    mainTrackerCanvases.[8,0] <- level9ColorCanvas
    let level9NumeralCanvas = Views.MakeLevel9View(Some(owInstance))
    gridAdd(mainTracker, level9NumeralCanvas, 8, 1) 
    mainTrackerCanvases.[8,1] <- level9NumeralCanvas
    level9NumeralCanvas.MouseEnter.Add(fun _ -> showLocator(ShowLocatorDescriptor.DungeonIndex 8))
    level9NumeralCanvas.MouseLeave.Add(fun _ -> hideLocator())
    let boxItemImpl(box:TrackerModel.Box, requiresForceUpdate) = 
        let c = Views.MakeBoxItem(cm, box)
        box.Changed.Add(fun _ -> if requiresForceUpdate then TrackerModel.forceUpdate())
        c.MouseEnter.Add(fun _ -> 
            match box.CellCurrent() with
            | 3 -> showLocatorInstanceFunc(owInstance.PowerBraceletable)
            | 4 -> showLocatorInstanceFunc(owInstance.Ladderable)
            | 7 -> showLocatorInstanceFunc(owInstance.Raftable)
            | 8 -> showLocatorInstanceFunc(owInstance.Whistleable)
            | 9 -> showLocatorInstanceFunc(owInstance.Burnable)
            | _ -> ()
            )
        c.MouseLeave.Add(fun _ -> hideLocator())
        timelineItems.Add(new Timeline.TimelineItem(fun()->if box.PlayerHas()=TrackerModel.PlayerHas.YES then Some(CustomComboBoxes.boxCurrentBMP(box.CellCurrent(), true)) else None))
        c
    // items
    let finalCanvasOf1Or4 = 
        if TrackerModel.IsHiddenDungeonNumbers() then
            null
        else        
            boxItemImpl(TrackerModel.DungeonTrackerInstance.TheDungeonTrackerInstance.FinalBoxOf1Or4, false)
    if TrackerModel.IsHiddenDungeonNumbers() then
        for i = 0 to 8 do
            for j = 0 to 2 do
                let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
                gridAdd(mainTracker, c, i, j+2)
                if j<>2 || i <> 8 then   // dungeon 9 does not have 3 items
                    canvasAdd(c, boxItemImpl(TrackerModel.GetDungeon(i).Boxes.[j], false), 0., 0.)
                mainTrackerCanvases.[i,j+2] <- c
    else
        for i = 0 to 8 do
            for j = 0 to 2 do
                let c = new Canvas(Width=30., Height=30., Background=System.Windows.Media.Brushes.Black)
                gridAdd(mainTracker, c, i, j+2)
                if j=0 || j=1 || i=7 then
                    canvasAdd(c, boxItemImpl(TrackerModel.GetDungeon(i).Boxes.[j], false), 0., 0.)
                mainTrackerCanvases.[i,j+2] <- c
    do 
        let RedrawForSecondQuestDungeonToggle() =
            if not(TrackerModel.IsHiddenDungeonNumbers()) then
                mainTrackerCanvases.[0,4].Children.Remove(finalCanvasOf1Or4) |> ignore
                mainTrackerCanvases.[3,4].Children.Remove(finalCanvasOf1Or4) |> ignore
                if TrackerModel.Options.IsSecondQuestDungeons.Value then
                    canvasAdd(mainTrackerCanvases.[3,4], finalCanvasOf1Or4, 0., 0.)
                else
                    canvasAdd(mainTrackerCanvases.[0,4], finalCanvasOf1Or4, 0., 0.)
        RedrawForSecondQuestDungeonToggle()
        OptionsMenu.secondQuestDungeonsOptionChanged.Publish.Add(fun _ -> RedrawForSecondQuestDungeonToggle())

    // in mixed quest, buttons to hide first/second quest
    let mutable firstQuestOnlyInterestingMarks = Array2D.zeroCreate 16 8
    let mutable secondQuestOnlyInterestingMarks = Array2D.zeroCreate 16 8
    let thereAreMarks(questOnlyInterestingMarks:_[,]) =
        let mutable r = false
        for x = 0 to 15 do 
            for y = 0 to 7 do
                if questOnlyInterestingMarks.[x,y] then
                    r <- true
        r
    let mutable hideFirstQuestFromMixed = fun _b -> ()
    let mutable hideSecondQuestFromMixed = fun _b -> ()

    let hideFirstQuestCheckBox  = new CheckBox(Content=new TextBox(Text="HFQ",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    hideFirstQuestCheckBox.ToolTip <- "Hide First Quest\nIn a mixed quest overworld tracker, shade out the first-quest-only spots.\nUseful if you're unsure if randomizer flags are mixed quest or second quest.\nCan't be used if you've marked a first-quest-only spot as having something."
    ToolTipService.SetShowDuration(hideFirstQuestCheckBox, 12000)
    let hideSecondQuestCheckBox = new CheckBox(Content=new TextBox(Text="HSQ",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    hideSecondQuestCheckBox.ToolTip <- "Hide Second Quest\nIn a mixed quest overworld tracker, shade out the second-quest-only spots.\nUseful if you're unsure if randomizer flags are mixed quest or first quest.\nCan't be used if you've marked a second-quest-only spot as having something."
    ToolTipService.SetShowDuration(hideSecondQuestCheckBox, 12000)

    hideFirstQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
    hideFirstQuestCheckBox.Checked.Add(fun _ -> 
        if thereAreMarks(firstQuestOnlyInterestingMarks) then
            System.Media.SystemSounds.Asterisk.Play()
            hideFirstQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
        else
            hideFirstQuestFromMixed false
        hideSecondQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
        )
    hideFirstQuestCheckBox.Unchecked.Add(fun _ -> hideFirstQuestFromMixed true)

    hideSecondQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
    hideSecondQuestCheckBox.Checked.Add(fun _ -> 
        if thereAreMarks(secondQuestOnlyInterestingMarks) then
            System.Media.SystemSounds.Asterisk.Play()
            hideSecondQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
        else
            hideSecondQuestFromMixed false
        hideFirstQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
        )
    hideSecondQuestCheckBox.Unchecked.Add(fun _ -> hideSecondQuestFromMixed true)
    if isMixed then
        canvasAdd(appMainCanvas, hideFirstQuestCheckBox,  WEBCAM_LINE + 10., 130.) 
        canvasAdd(appMainCanvas, hideSecondQuestCheckBox, WEBCAM_LINE + 60., 130.)

    let owItemGrid = makeGrid(5, 4, 30, 30)
    canvasAdd(appMainCanvas, owItemGrid, OW_ITEM_GRID_LOCATIONS.OFFSET, 30.)
    // ow 'take any' hearts
    for i = 0 to 3 do
        let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
        let redraw() = 
            c.Children.Clear()
            let curState = TrackerModel.playerProgressAndTakeAnyHearts.GetTakeAnyHeart(i)
            if curState=0 then canvasAdd(c, Graphics.BMPtoImage(Graphics.owHeartEmpty_bmp), 0., 0.)
            elif curState=1 then canvasAdd(c, Graphics.BMPtoImage(Graphics.owHeartFull_bmp), 0., 0.)
            else canvasAdd(c, Graphics.BMPtoImage(Graphics.owHeartEmpty_bmp), 0., 0.); CustomComboBoxes.placeSkippedItemXDecoration(c)
        redraw()
        TrackerModel.playerProgressAndTakeAnyHearts.TakeAnyHeartChanged.Add(fun n -> if n=i then redraw())
        let f b = TrackerModel.playerProgressAndTakeAnyHearts.SetTakeAnyHeart(i, (TrackerModel.playerProgressAndTakeAnyHearts.GetTakeAnyHeart(i) + (if b then 1 else -1) + 3) % 3)
        c.MouseLeftButtonDown.Add(fun _ -> f true)
        c.MouseRightButtonDown.Add(fun _ -> f false)
        c.MouseWheel.Add(fun x -> f (x.Delta<0))
        let HEARTX, HEARTY = OW_ITEM_GRID_LOCATIONS.HEARTS
        gridAdd(owItemGrid, c, HEARTX+i, HEARTY)
        timelineItems.Add(new Timeline.TimelineItem(fun()->if TrackerModel.playerProgressAndTakeAnyHearts.GetTakeAnyHeart(i)=1 then Some(Graphics.owHeartFull_bmp) else None))
    // ladder, armos, white sword items
    gridAddTuple(owItemGrid, Graphics.BMPtoImage Graphics.ladder_bmp, OW_ITEM_GRID_LOCATIONS.LADDER_ICON)
    let armos = Graphics.BMPtoImage Graphics.ow_key_armos_bmp
    armos.MouseEnter.Add(fun _ -> showLocatorInstanceFunc(owInstance.HasArmos))
    armos.MouseLeave.Add(fun _ -> hideLocator())
    gridAddTuple(owItemGrid, armos, OW_ITEM_GRID_LOCATIONS.ARMOS_ICON)
    let white_sword_canvas = new Canvas(Width=30., Height=30.)
    let redrawWhiteSwordCanvas(c:Canvas) =
        c.Children.Clear()
        if not(TrackerModel.playerComputedStateSummary.HaveWhiteSwordItem) &&           // don't have it yet
                TrackerModel.mapStateSummary.Sword2Location=TrackerModel.NOTFOUND &&    // have not found cave
                TrackerModel.GetLevelHint(9)<>TrackerModel.HintZone.UNKNOWN then        // have a hint
            canvasAdd(c, makeHintHighlight(21.), 4., 4.)
        canvasAdd(c, Graphics.BMPtoImage Graphics.white_sword_bmp, 4., 4.)
        Views.drawTinyIconIfLocationIsOverworldBlock(c, Some(owInstance), TrackerModel.mapStateSummary.Sword2Location)
    redrawWhiteSwordCanvas(white_sword_canvas)
    (*  don't need to do this, as redrawWhiteSwordCanvas() is currently called every doUIUpdate, heh
    // redraw after we can look up its new location coordinates
    let newLocation = Views.SynthesizeANewLocationKnownEvent(TrackerModel.mapSquareChoiceDomain.Changed |> Event.filter (fun (_,key) -> key=TrackerModel.MapSquareChoiceDomainHelper.SWORD2))
    newLocation.Add(fun _ -> redrawWhiteSwordCanvas())
    *)
    gridAddTuple(owItemGrid, white_sword_canvas, OW_ITEM_GRID_LOCATIONS.WHITE_SWORD_ICON)
    gridAddTuple(owItemGrid, boxItemImpl(TrackerModel.ladderBox, true), OW_ITEM_GRID_LOCATIONS.LADDER_ITEM_BOX)
    gridAddTuple(owItemGrid, boxItemImpl(TrackerModel.armosBox, false), OW_ITEM_GRID_LOCATIONS.ARMOS_ITEM_BOX)
    gridAddTuple(owItemGrid, boxItemImpl(TrackerModel.sword2Box, true), OW_ITEM_GRID_LOCATIONS.WHITE_SWORD_ITEM_BOX)
    white_sword_canvas.MouseEnter.Add(fun _ -> showLocator(ShowLocatorDescriptor.Sword2))
    white_sword_canvas.MouseLeave.Add(fun _ -> hideLocator())

    // brown sword, blue candle, blue ring, magical sword
    let veryBasicBoxImpl(bmp:System.Drawing.Bitmap, isTimeline, prop:TrackerModel.BoolProperty) =
        let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
        let no = CustomComboBoxes.no
        let yes = CustomComboBoxes.yes
        let rect = new System.Windows.Shapes.Rectangle(Width=30., Height=30., StrokeThickness=3.0)
        c.Children.Add(rect) |> ignore
        let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Transparent)  // just has item drawn on it, not the box
        c.Children.Add(innerc) |> ignore
        let redraw() =
            if prop.Value() then
                rect.Stroke <- yes
            else
                rect.Stroke <- no
        redraw()
        prop.Changed.Add(fun _ -> redraw())
        c.MouseLeftButtonDown.Add(fun _ ->
            prop.Toggle()
        )
        canvasAdd(innerc, Graphics.BMPtoImage bmp, 4., 4.)
        if isTimeline then
            timelineItems.Add(new Timeline.TimelineItem(fun()->if obj.Equals(rect.Stroke,yes) then Some(bmp) else None))
        c
    let basicBoxImpl(tts, img, prop) =
        let c = veryBasicBoxImpl(img, true, prop)
        c.ToolTip <- tts
        c
    let wood_sword_box = basicBoxImpl("Acquired wood sword (mark timeline)", Graphics.brown_sword_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodSword)    
    wood_sword_box.MouseEnter.Add(fun _ -> showLocator(ShowLocatorDescriptor.Sword1))
    wood_sword_box.MouseLeave.Add(fun _ -> hideLocator())
    gridAddTuple(owItemGrid, wood_sword_box, OW_ITEM_GRID_LOCATIONS.WOOD_SWORD_BOX)
    let wood_arrow_box = basicBoxImpl("Acquired wood arrow (mark timeline)", Graphics.wood_arrow_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodArrow)
    wood_arrow_box.MouseEnter.Add(fun _ -> showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.ARROW))
    wood_arrow_box.MouseLeave.Add(fun _ -> hideLocator())
    gridAddTuple(owItemGrid, wood_arrow_box, OW_ITEM_GRID_LOCATIONS.WOOD_ARROW_BOX)
    let blue_candle_box = basicBoxImpl("Acquired blue candle (mark timeline, affects routing)", Graphics.blue_candle_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueCandle)
    blue_candle_box.MouseEnter.Add(fun _ -> if TrackerModel.playerComputedStateSummary.CandleLevel=0 then showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.BLUE_CANDLE) else showLocatorInstanceFunc(owInstance.Burnable))
    blue_candle_box.MouseLeave.Add(fun _ -> hideLocator())
    gridAddTuple(owItemGrid, blue_candle_box, OW_ITEM_GRID_LOCATIONS.BLUE_CANDLE_BOX)
    let blue_ring_box = basicBoxImpl("Acquired blue ring (mark timeline)", Graphics.blue_ring_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueRing)
    blue_ring_box.MouseEnter.Add(fun _ -> showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.BLUE_RING))
    blue_ring_box.MouseLeave.Add(fun _ -> hideLocator())
    gridAddTuple(owItemGrid, blue_ring_box, OW_ITEM_GRID_LOCATIONS.BLUE_RING_BOX)
    let mags_box = basicBoxImpl("Acquired magical sword (mark timeline)", Graphics.magical_sword_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword)
    let mags_canvas = mags_box.Children.[1] :?> Canvas // a tiny bit fragile
    let redrawMagicalSwordCanvas(c:Canvas) =
        c.Children.Clear()
        if not(TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Value()) &&   // dont have sword
                TrackerModel.mapStateSummary.Sword3Location=TrackerModel.NOTFOUND &&           // not yet located cave
                TrackerModel.GetLevelHint(10)<>TrackerModel.HintZone.UNKNOWN then              // have a hint
            canvasAdd(c, makeHintHighlight(21.), 4., 4.)
        canvasAdd(c, Graphics.BMPtoImage Graphics.magical_sword_bmp, 4., 4.)
    redrawMagicalSwordCanvas(mags_canvas)
    gridAddTuple(owItemGrid, mags_box, OW_ITEM_GRID_LOCATIONS.MAGS_BOX)
    mags_box.MouseEnter.Add(fun _ -> showLocator(ShowLocatorDescriptor.Sword3))
    mags_box.MouseLeave.Add(fun _ -> hideLocator())
    // boomstick book, to mark when purchase in boomstick seed (normal book will become shield found in dungeon)
    let boom_book_box = basicBoxImpl("Purchased boomstick book (mark timeline)", Graphics.boom_book_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBoomBook)
    boom_book_box.MouseEnter.Add(fun _ -> showLocatorExactLocation(TrackerModel.mapStateSummary.BoomBookShopLocation))
    boom_book_box.MouseLeave.Add(fun _ -> hideLocator())
    gridAddTuple(owItemGrid, boom_book_box, OW_ITEM_GRID_LOCATIONS.BOOMSTICK_BOX)
    // mark the dungeon wins on timeline via ganon/zelda boxes
    gridAddTuple(owItemGrid, basicBoxImpl("Killed Ganon (mark timeline)",  Graphics.ganon_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasDefeatedGanon), OW_ITEM_GRID_LOCATIONS.GANON_BOX)
    gridAddTuple(owItemGrid, basicBoxImpl("Rescued Zelda (mark timeline)", Graphics.zelda_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasRescuedZelda),  OW_ITEM_GRID_LOCATIONS.ZELDA_BOX)
    TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasRescuedZelda.Changed.Add(fun b -> if b then notesTextBox.Text <- notesTextBox.Text + "\n" + timeTextBox.Text)
    // mark whether player currently has bombs, for overworld routing
    let bombIcon = veryBasicBoxImpl(Graphics.bomb_bmp, false, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBombs)
    bombIcon.MouseEnter.Add(fun _ -> showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.BOMB))
    bombIcon.MouseLeave.Add(fun _ -> hideLocator())
    bombIcon.ToolTip <- "Player currently has bombs (affects routing)"
    let BOMBX, BOMBY = OW_ITEM_GRID_LOCATIONS.LocateBomb()
    canvasAdd(appMainCanvas, bombIcon, BOMBX, BOMBY)

    // shield versus book icon (for boomstick flags/seeds)
    let toggleBookShieldCheckBox  = new CheckBox(Content=new TextBox(Text="S/B",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    toggleBookShieldCheckBox.ToolTip <- "Shield item icon instead of book item icon"
    toggleBookShieldCheckBox.IsChecked <- System.Nullable.op_Implicit false
    toggleBookShieldCheckBox.Checked.Add(fun _ -> TrackerModel.ToggleIsCurrentlyBook())
    toggleBookShieldCheckBox.Unchecked.Add(fun _ -> TrackerModel.ToggleIsCurrentlyBook())
    canvasAdd(appMainCanvas, toggleBookShieldCheckBox, OW_ITEM_GRID_LOCATIONS.OFFSET+150., 30.)

    let highlightOpenCaves = Graphics.BMPtoImage Graphics.openCaveIconBmp
    highlightOpenCaves.ToolTip <- "Highlight unmarked open caves"
    ToolTipService.SetPlacement(highlightOpenCaves, System.Windows.Controls.Primitives.PlacementMode.Top)
    highlightOpenCaves.MouseEnter.Add(fun _ -> showLocatorInstanceFunc(owInstance.Nothingable))
    highlightOpenCaves.MouseLeave.Add(fun _ -> hideLocator())
    canvasAdd(appMainCanvas, highlightOpenCaves, 540., 120.)

    // overworld map grouping, as main point of support for mirroring
    let mirrorOverworldFEs = ResizeArray<FrameworkElement>()   // overworldCanvas (on which all map is drawn) is here, as well as individual tiny textual/icon elements that need to be re-flipped
    let mutable displayIsCurrentlyMirrored = false
    let overworldCanvas = new Canvas(Width=OMTW*16., Height=11.*3.*8.)
    canvasAdd(appMainCanvas, overworldCanvas, 0., 150.)
    mirrorOverworldFEs.Add(overworldCanvas)

    // timer reset
    let timerResetButton = Graphics.makeButton("Pause/Reset timer", Some(16.), Some(Brushes.Orange))
    canvasAdd(appMainCanvas, timerResetButton, 12.8*OMTW, 60.)
    let mutable popupIsActive = false
    timerResetButton.Click.Add(fun _ ->
        if not popupIsActive then
            popupIsActive <- true
            let firstButton = Graphics.makeButton("Timer has been Paused.\nClick here to Resume.\n(Look below for Reset info.)", Some(16.), Some(Brushes.Orange))
            let secondButton = Graphics.makeButton("Timer has been Paused.\nClick here to confirm you want to Reset the timer,\nor click anywhere else to Resume.", Some(16.), Some(Brushes.Orange))
            let sp = new StackPanel(Orientation=Orientation.Vertical)
            sp.Children.Add(firstButton) |> ignore
            sp.Children.Add(new DockPanel(Height=300.)) |> ignore
            sp.Children.Add(secondButton) |> ignore
            let wh = new System.Threading.ManualResetEvent(false)
            firstButton.Click.Add(fun _ ->
                wh.Set() |> ignore
                )
            secondButton.Click.Add(fun _ ->
                resetTimerEvent.Trigger()
                wh.Set() |> ignore
                )
            async {
                TrackerModel.LastChangedTime.PauseAll()
                do! CustomComboBoxes.DoModal(cm, wh, 50., 200., sp)
                TrackerModel.LastChangedTime.ResumeAll()
                popupIsActive <- false
                } |> Async.StartImmediate
        )
    // spot summary
    let spotSummaryTB = new Border(Child=new TextBox(Text="Spot Summary", FontSize=16., IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.), Foreground=Brushes.Orange, Background=Brushes.Black), 
                                    BorderThickness=Thickness(1.), IsHitTestVisible=true, Background=Brushes.Black)
    let spotSummaryCanvas = new Canvas()
    spotSummaryTB.MouseEnter.Add(fun _ ->
        spotSummaryCanvas.Children.Clear()
        spotSummaryCanvas.Children.Add(OverworldMapTileCustomization.MakeRemainderSummaryDisplay()) |> ignore
        )   
    spotSummaryTB.MouseLeave.Add(fun _ -> spotSummaryCanvas.Children.Clear())
    canvasAdd(appMainCanvas, spotSummaryTB, 12.8*OMTW, 90.)

    let stepAnimateLink = LinkRouting.SetupLinkRouting(cm, changeCurrentRouteTarget, eliminateCurrentRouteTarget, isSpecificRouteTargetActive, updateNumberedTriforceDisplayImpl,
                                                        (fun() -> displayIsCurrentlyMirrored), MapStateProxy(14).DefaultInteriorBmp(), owInstance, redrawWhiteSwordCanvas, redrawMagicalSwordCanvas)

    let webcamLine = new Canvas(Background=Brushes.Orange, Width=2., Height=150., Opacity=0.4)
    canvasAdd(appMainCanvas, webcamLine, WEBCAM_LINE, 0.)

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    let THRU_MAIN_MAP_H = float(150 + 8*11*3)
    let THRU_MAP_AND_LEGEND_H = THRU_MAIN_MAP_H + float(11*3)
    let THRU_MAIN_MAP_AND_ITEM_PROGRESS_H = THRU_MAP_AND_LEGEND_H + 30.
    let START_DUNGEON_AND_NOTES_AREA_H = THRU_MAIN_MAP_AND_ITEM_PROGRESS_H
    let THRU_DUNGEON_AND_NOTES_AREA_H = START_DUNGEON_AND_NOTES_AREA_H + float(TH + 30 + (3 + 27*8 + 12*7 + 3) + 3)  // 3 is for a little blank space after this but before timeline
    let START_TIMELINE_H = THRU_DUNGEON_AND_NOTES_AREA_H
    let THRU_TIMELINE_H = START_TIMELINE_H + float TCH + 6.

    // ow map opaque fixed bottom layer
    let X_OPACITY = 0.55
    let owOpaqueMapGrid = makeGrid(16, 8, int OMTW, 11*3)
    owOpaqueMapGrid.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    for i = 0 to 15 do
        for j = 0 to 7 do
            let image = resizeMapTileImage(Graphics.BMPtoImage(owMapBMPs.[i,j]))
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            canvasAdd(c, image, 0., 0.)
            gridAdd(owOpaqueMapGrid, c, i, j)
            // shading between map tiles
            let OPA = 0.25
            let bottomShade = new Canvas(Width=OMTW, Height=float(3), Background=Brushes.Black, Opacity=OPA)
            canvasAdd(c, bottomShade, 0., float(10*3))
            let rightShade  = new Canvas(Width=float(3), Height=float(11*3), Background=Brushes.Black, Opacity=OPA)
            canvasAdd(c, rightShade, OMTW-3., 0.)
            // permanent icons
            if owInstance.AlwaysEmpty(i,j) then
                let icon = resizeMapTileImage <| Graphics.BMPtoImage(Graphics.theFullTileBmpTable.[TrackerModel.MapSquareChoiceDomainHelper.DARK_X].[0]) // "X"
                icon.Opacity <- X_OPACITY
                canvasAdd(c, icon, 0., 0.)
    canvasAdd(overworldCanvas, owOpaqueMapGrid, 0., 0.)

    // layer to place darkening icons - dynamic icons that are below route-drawing but above the fixed base layer
    // this layer is also used to draw map icons that get drawn below routing, such as potion shops
    let owDarkeningMapGrid = makeGrid(16, 8, int OMTW, 11*3)
    let owDarkeningMapGridCanvases = Array2D.zeroCreate 16 8
    owDarkeningMapGrid.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    for i = 0 to 15 do
        for j = 0 to 7 do
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            gridAdd(owDarkeningMapGrid, c, i, j)
            owDarkeningMapGridCanvases.[i,j] <- c
    canvasAdd(overworldCanvas, owDarkeningMapGrid, 0., 0.)

    // layer to place 'hiding' icons - dynamic darkening icons that are below route-drawing but above the previous layers
    let owHidingMapGrid = makeGrid(16, 8, int OMTW, 11*3)
    let owHidingMapGridCanvases = Array2D.zeroCreate 16 8
    owHidingMapGrid.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    for i = 0 to 15 do
        for j = 0 to 7 do
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            gridAdd(owHidingMapGrid, c, i, j)
            owHidingMapGridCanvases.[i,j] <- c
    canvasAdd(overworldCanvas, owHidingMapGrid, 0., 0.)
    let hide(x,y) =
        let hideColor = Brushes.DarkSlateGray // Brushes.Black
        let hideOpacity = 0.6 // 0.4
        let rect = new System.Windows.Shapes.Rectangle(Width=7.0*OMTW/48., Height=float(11*3)-1.5, Stroke=hideColor, StrokeThickness = 3., Fill=hideColor, Opacity=hideOpacity)
        canvasAdd(owHidingMapGridCanvases.[x,y], rect, 7.*OMTW/48., 0.)
        let rect = new System.Windows.Shapes.Rectangle(Width=7.0*OMTW/48., Height=float(11*3)-1.5, Stroke=hideColor, StrokeThickness = 3., Fill=hideColor, Opacity=hideOpacity)
        canvasAdd(owHidingMapGridCanvases.[x,y], rect, 19.*OMTW/48., 0.)
        let rect = new System.Windows.Shapes.Rectangle(Width=7.0*OMTW/48., Height=float(11*3)-1.5, Stroke=hideColor, StrokeThickness = 3., Fill=hideColor, Opacity=hideOpacity)
        canvasAdd(owHidingMapGridCanvases.[x,y], rect, 32.*OMTW/48., 0.)
    hideSecondQuestFromMixed <- 
        (fun unhide ->  // make mixed appear reduced to 1st quest
            for x = 0 to 15 do
                for y = 0 to 7 do
                    if OverworldData.owMapSquaresSecondQuestOnly.[y].Chars(x) = 'X' then
                        if unhide then
                            owHidingMapGridCanvases.[x,y].Children.Clear()
                        else
                            hide(x,y)
        )
    hideFirstQuestFromMixed <-
        (fun unhide ->   // make mixed appear reduced to 2nd quest
            for x = 0 to 15 do
                for y = 0 to 7 do
                    if OverworldData.owMapSquaresFirstQuestOnly.[y].Chars(x) = 'X' then
                        if unhide then
                            owHidingMapGridCanvases.[x,y].Children.Clear()
                        else
                            hide(x,y)
        )

    // ow route drawing layer
    routeDrawingCanvas.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    canvasAdd(overworldCanvas, routeDrawingCanvas, 0., 0.)

    // nearby ow tiles magnified overlay
    let ENLARGE = 8.
    let BT = 2.  // border thickness of the interior 3x3 grid of tiles
    let dungeonTabsOverlay = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(5.), Background=Brushes.Black, Opacity=0., IsHitTestVisible=false)
    let DTOCW,DTOCH = 3.*16.*ENLARGE + 4.*BT, 3.*11.*ENLARGE + 4.*BT
    let dungeonTabsOverlayContent = new Canvas(Width=DTOCW, Height=DTOCH)
    mirrorOverworldFEs.Add(dungeonTabsOverlayContent)
    dungeonTabsOverlay.Child <- dungeonTabsOverlayContent
    let overlayTiles = Array2D.zeroCreate 16 8
    for i = 0 to 15 do
        for j = 0 to 7 do
            let bmp = new System.Drawing.Bitmap(16*int ENLARGE, 11*int ENLARGE)
            for x = 0 to 15 do
                for y = 0 to 10 do
                    let c = owMapBMPs.[i,j].GetPixel(x*3, y*3)
                    for px = 0 to int ENLARGE - 1 do
                        for py = 0 to int ENLARGE - 1 do
                            // diagonal rocks
                            let c = 
                                // The diagonal rock data is based on the first quest map. A few screens are different in 2nd/mixed quest.
                                // So we apply a kludge to load the correct diagonal data.
                                let i,j = 
                                    if owMapNum=1 && i=4 && j=7 then // second quest has a cave like 14,5 here
                                        14,5
                                    elif owMapNum=1 && i=11 && j=0 then // second quest has dead fairy here, borrow 2,4
                                        2,4
                                    elif owMapNum<>0 && i=12 && j=3 then // non-first quest has a whistle lake here, borrow 2,4
                                        2,4
                                    else
                                        i,j
                                if OverworldData.owNEupperRock.[i,j].[x,y] then
                                    if px+py > int ENLARGE - 1 then 
                                        owMapBMPs.[i,j].GetPixel(x*3, (y+1)*3)
                                    else 
                                        c
                                elif OverworldData.owSEupperRock.[i,j].[x,y] then
                                    if px < py then 
                                        owMapBMPs.[i,j].GetPixel(x*3, (y+1)*3)
                                    else 
                                        c
                                elif OverworldData.owNElowerRock.[i,j].[x,y] then
                                    if px+py < int ENLARGE - 1 then 
                                        owMapBMPs.[i,j].GetPixel(x*3, (y-1)*3)
                                    else 
                                        c
                                elif OverworldData.owSElowerRock.[i,j].[x,y] then
                                    if px > py then 
                                        owMapBMPs.[i,j].GetPixel(x*3, (y-1)*3)
                                    else 
                                        c
                                else 
                                    c
                            // edges of squares
                            let c = 
                                if (px+1) % int ENLARGE = 0 || (py+1) % int ENLARGE = 0 then
                                    System.Drawing.Color.FromArgb(int c.R / 2, int c.G / 2, int c.B / 2)
                                else
                                    c
                            bmp.SetPixel(x*int ENLARGE + px, y*int ENLARGE + py, c)
            overlayTiles.[i,j] <- Graphics.BMPtoImage bmp
    // ow map -> dungeon tabs interaction
    let selectDungeonTabEvent = new Event<_>()
    let mutable mostRecentlyScrolledDungeonIndex = -1
    let mostRecentlyScrolledDungeonIndexTime = new TrackerModel.LastChangedTime()
    // ow map
    let owMapGrid = makeGrid(16, 8, int OMTW, 11*3)
    let owCanvases = Array2D.zeroCreate 16 8
    let owUpdateFunctions = Array2D.create 16 8 (fun _ _ -> ())
    let drawRectangleCornersHighlight(c,x,y,color) =
        ignore(c,x,y,color)
        (*
        // full rectangles badly obscure routing paths, so we just draw corners
        let L1,L2,R1,R2 = 0.0, (OMTW-4.)/2.-6., (OMTW-4.)/2.+6., OMTW-4.
        let T1,T2,B1,B2 = 0.0, 10.0, 19.0, 29.0
        let s = new System.Windows.Shapes.Line(X1=L1, X2=L2, Y1=T1+1.5, Y2=T1+1.5, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=L1+1.5, X2=L1+1.5, Y1=T1, Y2=T2, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=L1, X2=L2, Y1=B2-1.5, Y2=B2-1.5, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=L1+1.5, X2=L1+1.5, Y1=B1, Y2=B2, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=R1, X2=R2, Y1=T1+1.5, Y2=T1+1.5, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=R2-1.5, X2=R2-1.5, Y1=T1, Y2=T2, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=R1, X2=R2, Y1=B2-1.5, Y2=B2-1.5, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=R2-1.5, X2=R2-1.5, Y1=B1, Y2=B2, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        *)
    let drawDungeonHighlight(c,x,y) =
        drawRectangleCornersHighlight(c,x,y,System.Windows.Media.Brushes.Yellow)
    let drawCompletedIconHighlight(c,x,y) =
        let rect = new System.Windows.Shapes.Rectangle(Width=15.0*OMTW/48., Height=27.0, Stroke=System.Windows.Media.Brushes.Black, StrokeThickness = 3.,
                                                        Fill=System.Windows.Media.Brushes.Black, Opacity=0.4, IsHitTestVisible=false)
        let diff = if displayIsCurrentlyMirrored then 18.0*OMTW/48. else 15.0*OMTW/48.
        canvasAdd(c, rect, x*OMTW+diff, float(y*11*3)+3.0)
    let drawCompletedDungeonHighlight(c,x,y) =
        // darkened rectangle corners
        let yellow = System.Windows.Media.Brushes.Yellow.Color
        let darkYellow = Color.FromRgb(yellow.R/2uy, yellow.G/2uy, yellow.B/2uy)
        drawRectangleCornersHighlight(c,x,y,new SolidColorBrush(darkYellow))
        // darken the number
        drawCompletedIconHighlight(c,x,y)
    let drawWarpHighlight(c,x,y) =
        drawRectangleCornersHighlight(c,x,y,System.Windows.Media.Brushes.Orchid)
    let drawDarkening(c,x,y) =
        let rect = new System.Windows.Shapes.Rectangle(Width=OMTW, Height=float(11*3), Stroke=System.Windows.Media.Brushes.Black, StrokeThickness = 3.,
                                                        Fill=System.Windows.Media.Brushes.Black, Opacity=X_OPACITY)
        canvasAdd(c, rect, x*OMTW, float(y*11*3))
    let drawDungeonRecorderWarpHighlight(c,x,y) =
        drawRectangleCornersHighlight(c,x,y,System.Windows.Media.Brushes.Lime)
    for i = 0 to 15 do
        for j = 0 to 7 do
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            gridAdd(owMapGrid, c, i, j)
            // we need a dummy image to make the canvas absorb the mouse interactions, so just re-draw the map at almost 0 opacity
            let image = resizeMapTileImage <| Graphics.BMPtoImage(owMapBMPs.[i,j])
            image.Opacity <- 0.001
            canvasAdd(c, image, 0., 0.)
            // highlight mouse, do mouse-sensitive stuff
            let rect = new System.Windows.Shapes.Rectangle(Width=OMTW-4., Height=float(11*3)-4., Stroke=System.Windows.Media.Brushes.White)
            c.MouseEnter.Add(fun ea ->  canvasAdd(c, rect, 2., 2.)
                                        // draw routes
                                        let mousePos = ea.GetPosition(c)
                                        let mousePos = if displayIsCurrentlyMirrored then Point(OMTW - mousePos.X, mousePos.Y) else mousePos
                                        drawRoutesTo(currentRouteTarget(), routeDrawingCanvas, mousePos, i, j, TrackerModel.Options.Overworld.DrawRoutes.Value, if TrackerModel.Options.Overworld.HighlightNearby.Value then OverworldRouteDrawing.MaxYGH else 0)
                                        // show enlarged version of current & nearby rooms
                                        dungeonTabsOverlayContent.Children.Clear()
                                        // fill whole canvas black, so elements behind don't show through
                                        canvasAdd(dungeonTabsOverlayContent, new Shapes.Rectangle(Width=dungeonTabsOverlayContent.Width, Height=dungeonTabsOverlayContent.Height, Fill=Brushes.Black), 0., 0.)
                                        let xmin = min (max (i-1) 0) 13
                                        let ymin = min (max (j-1) 0) 5
                                        // draw a white highlight rectangle behind the tile where mouse is
                                        let rect = new Shapes.Rectangle(Width=16.*ENLARGE + 2.*BT, Height=11.*ENLARGE + 2.*BT, Fill=Brushes.White)
                                        canvasAdd(dungeonTabsOverlayContent, rect, float (i-xmin)*(16.*ENLARGE+BT), float (j-ymin)*(11.*ENLARGE+BT))
                                        // draw the 3x3 tiles
                                        for x = 0 to 2 do
                                            for y = 0 to 2 do
                                                canvasAdd(dungeonTabsOverlayContent, overlayTiles.[xmin+x,ymin+y], BT+float x*(16.*ENLARGE+BT), BT+float y*(11.*ENLARGE+BT))
                                        if TrackerModel.Options.Overworld.ShowMagnifier.Value then 
                                            dungeonTabsOverlay.Opacity <- 1.0
                                        // track current location for F5 & speech recognition purposes
                                        currentlyMousedOWX <- i
                                        currentlyMousedOWY <- j
                                        )
            c.MouseLeave.Add(fun _ -> c.Children.Remove(rect) |> ignore
                                      dungeonTabsOverlayContent.Children.Clear()
                                      dungeonTabsOverlay.Opacity <- 0.
                                      routeDrawingCanvas.Children.Clear())
            // icon
            if owInstance.AlwaysEmpty(i,j) then
                // already set up as permanent opaque layer, in code above, so nothing else to do
                // except...
                if i=9 && j=3 || i=3 && j=4 then // fairy spots
                    let image = Graphics.BMPtoImage Graphics.fairy_bmp
                    canvasAdd(c, image, OMTW/2.-8., 1.)
                if i=15 && j=5 then // ladder spot
                    let extraDecorationsF(boxPos:Point) =
                        // ladderBox position in main canvas
                        let lx,ly = OW_ITEM_GRID_LOCATIONS.Locate(OW_ITEM_GRID_LOCATIONS.LADDER_ITEM_BOX)
                        OverworldMapTileCustomization.computeExtraDecorationArrow(lx, ly, boxPos)
                    let coastBoxOnOwGrid = Views.MakeBoxItemWithExtraDecorations(cm, TrackerModel.ladderBox, false, extraDecorationsF)
                    mirrorOverworldFEs.Add(coastBoxOnOwGrid)
                    canvasAdd(c, coastBoxOnOwGrid, OMTW-31., 1.)
                    //TrackerModel.ladderBox.Changed.Add(fun _ -> // this would make the box go away instantly once got
                    doUIUpdateEvent.Publish.Add(fun _ ->          // this waits a second, which gives visual feeback when left-clicked & allows hotkeys to skip it via multi-hotkey
                        if TrackerModel.ladderBox.PlayerHas() = TrackerModel.PlayerHas.NO then
                            coastBoxOnOwGrid.Opacity <- 1.
                            coastBoxOnOwGrid.IsHitTestVisible <- true
                        else
                            coastBoxOnOwGrid.Opacity <- 0.
                            coastBoxOnOwGrid.IsHitTestVisible <- false
                        )
            else
                let redrawGridSpot() =
                    // cant remove-by-identity because of non-uniques; remake whole canvas
                    owDarkeningMapGridCanvases.[i,j].Children.Clear()
                    c.Children.Clear()
                    // we need a dummy image to make the canvas absorb the mouse interactions, so just re-draw the map at almost 0 opacity
                    let image = resizeMapTileImage <| Graphics.BMPtoImage(owMapBMPs.[i,j])
                    image.Opacity <- 0.001
                    canvasAdd(c, image, 0., 0.)
                    let ms = MapStateProxy(TrackerModel.overworldMapMarks.[i,j].Current())
                    let iconBMP,extraDecorations = GetIconBMPAndExtraDecorations(cm,ms,i,j)
                    // be sure to draw in appropriate layer
                    if iconBMP <> null then 
                        let icon = resizeMapTileImage(Graphics.BMPtoImage iconBMP)
                        if ms.IsX then
                            icon.Opacity <- X_OPACITY
                            canvasAdd(owDarkeningMapGridCanvases.[i,j], icon, 0., 0.)  // the icon 'is' the darkening
                        else
                            icon.Opacity <- 1.0
                            drawDarkening(owDarkeningMapGridCanvases.[i,j], 0., 0)     // darken below icon and routing marks
                            canvasAdd(c, icon, 0., 0.)                                 // add icon above routing
                            for fe,x,y in extraDecorations do
                                canvasAdd(c, fe, x, y)
                    if ms.IsDungeon then
                        drawDungeonHighlight(c,0.,0)
                    if ms.IsWarp then
                        drawWarpHighlight(c,0.,0)
                let isLegalHere(state) = if state = TrackerModel.MapSquareChoiceDomainHelper.ARMOS then owInstance.HasArmos(i,j) else true
                let updateGridSpot delta phrase = 
                    async {
                        // figure out what new state we just interacted-to
                        if delta = 777 then 
                            let curState = TrackerModel.overworldMapMarks.[i,j].Current()
                            if curState = -1 then
                                // if unmarked, use voice to set new state
                                match speechRecognitionInstance.ConvertSpokenPhraseToMapCell(phrase) with
                                | Some newState -> 
                                    if isLegalHere(newState) && TrackerModel.overworldMapMarks.[i,j].AttemptToSet(newState) then
                                        if newState >=0 && newState <=8 then
                                            selectDungeonTabEvent.Trigger(newState)
                                        Graphics.PlaySoundForSpeechRecognizedAndUsedToMark()
                                        let pos = c.TranslatePoint(Point(OMTW/2., 11.*3./2.), appMainCanvas)
                                        match overworldAcceleratorTable.TryGetValue(newState) with
                                        | (true,f) -> 
                                            do! f(cm,c,i,j)
                                            Graphics.WarpMouseCursorTo(pos)
                                        | _ -> ()
                                | None -> ()
                            elif MapStateProxy(curState).IsThreeItemShop && TrackerModel.getOverworldMapExtraData(i,j,TrackerModel.MapSquareChoiceDomainHelper.SHOP)=0 then
                                // if item shop with only one item marked, use voice to set other item
                                match speechRecognitionInstance.ConvertSpokenPhraseToMapCell(phrase) with
                                | Some newState -> 
                                    if TrackerModel.MapSquareChoiceDomainHelper.IsItem(newState) then
                                        TrackerModel.setOverworldMapExtraData(i,j,TrackerModel.MapSquareChoiceDomainHelper.SHOP,TrackerModel.MapSquareChoiceDomainHelper.ToItem(newState))
                                        Graphics.PlaySoundForSpeechRecognizedAndUsedToMark()
                                | None -> ()
                        elif delta = 1 then
                            TrackerModel.overworldMapMarks.[i,j].Next()
                            while not(isLegalHere(TrackerModel.overworldMapMarks.[i,j].Current())) do TrackerModel.overworldMapMarks.[i,j].Next()
                            let newState = TrackerModel.overworldMapMarks.[i,j].Current()
                            if newState >=0 && newState <=7 then
                                mostRecentlyScrolledDungeonIndex <- newState
                                mostRecentlyScrolledDungeonIndexTime.SetNow()
                        elif delta = -1 then 
                            TrackerModel.overworldMapMarks.[i,j].Prev() 
                            while not(isLegalHere(TrackerModel.overworldMapMarks.[i,j].Current())) do TrackerModel.overworldMapMarks.[i,j].Prev()
                            let newState = TrackerModel.overworldMapMarks.[i,j].Current()
                            if newState >=0 && newState <=7 then
                                mostRecentlyScrolledDungeonIndex <- newState
                                mostRecentlyScrolledDungeonIndexTime.SetNow()
                        elif delta = 0 then 
                            ()
                        else failwith "bad delta"
                        let ms = MapStateProxy(TrackerModel.overworldMapMarks.[i,j].Current())
                        if OverworldData.owMapSquaresSecondQuestOnly.[j].Chars(i) = 'X' then
                            secondQuestOnlyInterestingMarks.[i,j] <- ms.IsInteresting 
                        if OverworldData.owMapSquaresFirstQuestOnly.[j].Chars(i) = 'X' then
                            firstQuestOnlyInterestingMarks.[i,j] <- ms.IsInteresting 
                        redrawGridSpot()
                        } |> Async.StartImmediate
                owUpdateFunctions.[i,j] <- updateGridSpot 
                owCanvases.[i,j] <- c
                mirrorOverworldFEs.Add(c)
                mirrorOverworldFEs.Add(owDarkeningMapGridCanvases.[i,j])
                let popupIsActive = ref false
                let SetNewValue(currentState, originalState) = async {
                    if isLegalHere(currentState) && TrackerModel.overworldMapMarks.[i,j].AttemptToSet(currentState) then
                        if currentState >=0 && currentState <=8 then
                            selectDungeonTabEvent.Trigger(currentState)
                        match overworldAcceleratorTable.TryGetValue(currentState) with
                        | (true,f) -> do! f(cm,c,i,j)
                        | _ -> ()
                        redrawGridSpot()
                        if originalState = -1 && currentState <> -1 then doUIUpdateEvent.Trigger()  // immediate update to dismiss green/yellow highlight from current tile
                    else
                        System.Media.SystemSounds.Asterisk.Play()  // e.g. they tried to set armos on non-armos, or tried to set Level1 when already found elsewhere
                }
                let activatePopup(activationDelta) =
                    popupIsActive := true
                    let GCOL,GROW = 8,5
                    let GCOUNT = GCOL*GROW
                    let pos = c.TranslatePoint(Point(), appMainCanvas)
                    let ST = CustomComboBoxes.borderThickness
                    let tileImage = resizeMapTileImage <| Graphics.BMPtoImage(owMapBMPs.[i,j])
                    let tileCanvas = new Canvas(Width=OMTW, Height=11.*3.)
                    let originalState = TrackerModel.overworldMapMarks.[i,j].Current()
                    let gridWidth = float(GCOL*(5*3+2*int ST)+int ST)
                    let gridxPosition = 
                        if pos.X < OMTW*2. then 
                            -ST // left align
                        elif pos.X > OMTW*13. then 
                            OMTW - gridWidth  // right align
                        else
                            (OMTW - gridWidth)/2.  // center align
                    let typicalGESAI(n) : FrameworkElement*_*_ =
                        let isSelectable = ((n = originalState) || TrackerModel.mapSquareChoiceDomain.CanAddUse(n)) && isLegalHere(n)
                        upcast Graphics.BMPtoImage(MapStateProxy(n).DefaultInteriorBmp()), isSelectable, n
                    let gridElementsSelectablesAndIDs : (FrameworkElement*bool*int)[] = [|
                        // three full rows
                        for n = 0 to 23 do
                            yield typicalGESAI(n)
                        // money row
                        yield null, false, -999  // null asks selector to 'leave a hole' here
                        for n = 24 to 29 do
                            yield typicalGESAI(n)
                        yield null, false, -999  // null asks selector to 'leave a hole' here
                        // other row
                        for n = 30 to 34 do
                            yield typicalGESAI(n)
                        yield upcast new Canvas(Width=5.*3., Height=9.*3., Background=Graphics.overworldCommonestFloorColorBrush, Opacity=X_OPACITY), true, 35
                        yield upcast new Canvas(Width=5.*3., Height=9.*3., Background=Graphics.overworldCommonestFloorColorBrush), true, -1
                        yield null, false, -999  // null asks selector to 'leave a hole' here
                        |]
                    let shopsOnTop = TrackerModel.Options.Overworld.ShopsFirst.Value // start with shops, rather than dungeons, on top of grid
                    let gridElementsSelectablesAndIDs = 
                        if shopsOnTop then [| yield! gridElementsSelectablesAndIDs.[16..]; yield! gridElementsSelectablesAndIDs.[..15] |] else gridElementsSelectablesAndIDs
                    let originalStateIndex = gridElementsSelectablesAndIDs |> Array.findIndex (fun (_,_,s) -> s = originalState)
                    if gridElementsSelectablesAndIDs.Length <> GCOUNT then
                        failwith "bad ow grid tile layout"
                    async {
                        let! r = CustomComboBoxes.DoModalGridSelect(cm, pos.X, pos.Y, tileCanvas,
                                    gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (GCOL, GROW, 5*3, 9*3), gridxPosition, 11.*3.+ST,
                                    (fun (currentState) -> 
                                        tileCanvas.Children.Clear()
                                        canvasAdd(tileCanvas, tileImage, 0., 0.)
                                        let bmp,_ = GetIconBMPAndExtraDecorations(cm, MapStateProxy(currentState), i, j)
                                        if bmp <> null then
                                            let icon = bmp |> Graphics.BMPtoImage |> resizeMapTileImage
                                            if MapStateProxy(currentState).IsX then
                                                icon.Opacity <- X_OPACITY
                                            canvasAdd(tileCanvas, icon, 0., 0.)
                                        let s = if currentState = -1 then "Unmarked" else let _,_,s = TrackerModel.dummyOverworldTiles.[currentState] in s
                                        let text = new TextBox(Text=s, Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.),
                                                                    FontSize=16., HorizontalContentAlignment=HorizontalAlignment.Center)
                                        let textBorder = new Border(BorderThickness=Thickness(ST), Child=text, Background=Brushes.Black, BorderBrush=Brushes.Gray)
                                        let dp = new DockPanel(LastChildFill=false, Width=gridWidth)
                                        DockPanel.SetDock(textBorder, Dock.Bottom)
                                        dp.Children.Add(textBorder) |> ignore
                                        Canvas.SetBottom(dp, 33.)
                                        Canvas.SetLeft(dp, gridxPosition)
                                        tileCanvas.Children.Add(dp) |> ignore
                                        ),
                                    (fun (_ea, currentState) -> CustomComboBoxes.DismissPopupWithResult(currentState)),
                                    [], CustomComboBoxes.ModalGridSelectBrushes.Defaults(), true)
                        match r with
                        | Some(currentState) -> do! SetNewValue(currentState, originalState)
                        | None -> ()
                        popupIsActive := false
                        } |> Async.StartImmediate
                c.MouseRightButtonDown.Add(fun _ -> 
                    if not !popupIsActive then
                        // right click activates the popup selector
                        activatePopup(0)
                    )
                c.MouseLeftButtonDown.Add(fun _ -> 
                    if not !popupIsActive then
                        // left click is the 'special interaction'
                        let pos = c.TranslatePoint(Point(), appMainCanvas)
                        let msp = MapStateProxy(TrackerModel.overworldMapMarks.[i,j].Current())
                        if msp.IsX then
                            activatePopup(0)  // thus, if you have unmarked, then left-click left-click pops up, as the first marks X, and the second now pops up
                        else
                            async {
                                let! needRedraw, needUIUpdate = DoLeftClick(cm,msp,i,j,pos,popupIsActive)
                                if needRedraw then redrawGridSpot()
                                if needUIUpdate then doUIUpdateEvent.Trigger()  // immediate update to dismiss green/yellow highlight from current tile
                            } |> Async.StartImmediate
                    )
                c.MouseWheel.Add(fun x -> if not !popupIsActive then activatePopup(if x.Delta<0 then 1 else -1))
                c.MyKeyAdd(fun ea ->
                    if not !popupIsActive then
                        match HotKeys.OverworldHotKeyProcessor.TryGetValue(ea.Key) with
                        | Some(hotKeyedState) -> 
                            ea.Handled <- true
                            let originalState = TrackerModel.overworldMapMarks.[i,j].Current()
                            let state = OverworldMapTileCustomization.DoSpecialHotKeyHandlingForOverworldTiles(i, j, originalState, hotKeyedState)
                            Async.StartImmediate <| SetNewValue(state, originalState)
                        | None -> ()
                    )
    speechRecognitionInstance.AttachSpeechRecognizedToApp(appMainCanvas, (fun recognizedText ->
                                if currentlyMousedOWX >= 0 then // can hear speech before we have moused over any (uninitialized location)
                                    let c = owCanvases.[currentlyMousedOWX,currentlyMousedOWY]
                                    if c <> null && c.IsMouseOver then  // canvas can be null for always-empty grid places
                                        // Note: IsMouseOver appears only be true if we are along the way from a mouse click to the visual root.
                                        // As a result, the update never fires when a popup window is active, because the popup sunglasses would always 
                                        // intercept the click in a different part of the Visual tree.  This is good, because we don't want speech 
                                        // mutating the world while a popup is active (the popup's modality should block speech).
                                        // I guess we could also ask 'cm' if a popup is active.
                                        owUpdateFunctions.[currentlyMousedOWX,currentlyMousedOWY] 777 recognizedText
                            ))
    canvasAdd(overworldCanvas, owMapGrid, 0., 0.)
    owMapGrid.MouseLeave.Add(fun _ -> ensureRespectingOwGettableScreensCheckBox())

    let recorderingCanvas = new Canvas(Width=16.*OMTW, Height=float(8*11*3))  // really the 'extra top layer' canvas for adding final marks to overworld map
    recorderingCanvas.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    canvasAdd(overworldCanvas, recorderingCanvas, 0., 0.)
    let makeStartIcon() = new System.Windows.Shapes.Ellipse(Width=float(11*3)-2., Height=float(11*3)-2., Stroke=System.Windows.Media.Brushes.Lime, StrokeThickness=3.0, IsHitTestVisible=false)
    let startIcon = makeStartIcon()

    // map legend
    let LEFT_OFFSET = 78.0
    let legendCanvas = new Canvas()
    canvasAdd(appMainCanvas, legendCanvas, LEFT_OFFSET, THRU_MAIN_MAP_H)

    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="The LEGEND\nof Z-Tracker")
    canvasAdd(appMainCanvas, tb, 0., THRU_MAIN_MAP_H)

    let shrink(bmp) = resizeMapTileImage <| Graphics.BMPtoImage bmp
    let firstDungeonBMP = if TrackerModel.IsHiddenDungeonNumbers() then Graphics.theFullTileBmpTable.[0].[2] else Graphics.theFullTileBmpTable.[0].[0]
    canvasAdd(legendCanvas, shrink firstDungeonBMP, 0., 0.)
    drawDungeonHighlight(legendCanvas,0.,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Active\nDungeon")
    canvasAdd(legendCanvas, tb, OMTW*0.8, 0.)

    let firstGreenDungeonBMP = if TrackerModel.IsHiddenDungeonNumbers() then Graphics.theFullTileBmpTable.[0].[3] else Graphics.theFullTileBmpTable.[0].[1]
    canvasAdd(legendCanvas, shrink firstDungeonBMP, 2.1*OMTW, 0.)
    drawDungeonHighlight(legendCanvas,2.1,0)
    drawCompletedDungeonHighlight(legendCanvas,2.1,0)
    canvasAdd(legendCanvas, shrink firstGreenDungeonBMP, 2.5*OMTW, 0.)
    drawDungeonHighlight(legendCanvas,2.5,0)
    drawCompletedDungeonHighlight(legendCanvas,2.5,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Completed\nDungeon")
    canvasAdd(legendCanvas, tb, 3.3*OMTW, 0.)

    canvasAdd(legendCanvas, shrink firstGreenDungeonBMP, 4.8*OMTW, 0.)
    drawDungeonHighlight(legendCanvas,4.8,0)
    drawDungeonRecorderWarpHighlight(legendCanvas,4.8,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Recorder\nDestination")
    canvasAdd(legendCanvas, tb, 5.6*OMTW, 0.)

    canvasAdd(legendCanvas, shrink(Graphics.theFullTileBmpTable.[9].[0]), 7.1*OMTW, 0.)
    drawWarpHighlight(legendCanvas,7.1,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Any Road\n(Warp)")
    canvasAdd(legendCanvas, tb, 7.9*OMTW, 0.)

    let legendStartIconButtonCanvas = new Canvas(Background=Graphics.almostBlack, Width=OMTW*1.7, Height=11.*3.)
    let legendStartIcon = makeStartIcon()
    canvasAdd(legendStartIconButtonCanvas, legendStartIcon, 0.+8.5*OMTW/48., 0.)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Graphics.almostBlack, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Start\nSpot", IsHitTestVisible=false)
    canvasAdd(legendStartIconButtonCanvas, tb, 1.*OMTW, 0.)
    let legendStartIconButton = new Button(Content=legendStartIconButtonCanvas)
    canvasAdd(legendCanvas, legendStartIconButton, 9.4*OMTW, 0.)
    let mutable popupIsActive = false
    legendStartIconButton.Click.Add(fun _ ->
        if not popupIsActive then
            popupIsActive <- true
            let tb = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, FontSize=16.,
                                    Text="Click an overworld map tile to move the Start Spot icon there, or click anywhere outside the map to cancel")
            let element = new Canvas(Width=OMTW*16., Height=float(8*11*3), Background=Brushes.Transparent, IsHitTestVisible=true)
            canvasAdd(element, tb, 0., -30.)
            let hoverIcon = makeStartIcon()
            element.MouseLeave.Add(fun _ -> element.Children.Remove(hoverIcon))
            element.MouseMove.Add(fun ea ->
                let mousePos = ea.GetPosition(element)
                let i = int(mousePos.X / OMTW)
                let j = int(mousePos.Y / (11.*3.))
                element.Children.Remove(hoverIcon)
                canvasAdd(element, hoverIcon, float i*OMTW + 8.5*OMTW/48., float(j*11*3))
                )
            let wh = new System.Threading.ManualResetEvent(false)
            element.MouseDown.Add(fun ea ->
                let mousePos = ea.GetPosition(element)
                let i = int(mousePos.X / OMTW)
                let j = int(mousePos.Y / (11.*3.))
                if i>=0 && i<=15 && j>=0 && j<=7 then
                    TrackerModel.startIconX <- if displayIsCurrentlyMirrored then (15-i) else i
                    TrackerModel.startIconY <- j
                    doUIUpdateEvent.Trigger()
                    wh.Set() |> ignore
                )
            async {
                do! CustomComboBoxes.DoModal(cm, wh, 0., 150., element)
                popupIsActive <- false
                } |> Async.StartImmediate
        )

    // item progress
    let itemProgressCanvas = new Canvas(Width=16.*OMTW, Height=30.)
    let ITEM_PROGRESS_FIRST_ITEM = 130.
    canvasAdd(appMainCanvas, itemProgressCanvas, 0., THRU_MAP_AND_LEGEND_H)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Item Progress", IsHitTestVisible=false)
    canvasAdd(appMainCanvas, tb, 50., THRU_MAP_AND_LEGEND_H + 4.)
    itemProgressCanvas.MouseMove.Add(fun ea ->
        let pos = ea.GetPosition(itemProgressCanvas)
        let x = pos.X - ITEM_PROGRESS_FIRST_ITEM
        if x >  30. && x <  60. then
            showLocatorInstanceFunc(owInstance.Burnable)
        if x > 240. && x < 270. then
            showLocatorInstanceFunc(owInstance.Ladderable)
        if x > 270. && x < 300. then
            showLocatorInstanceFunc(owInstance.Whistleable)
        if x > 300. && x < 330. then
            showLocatorInstanceFunc(owInstance.PowerBraceletable)
        if x > 330. && x < 360. then
            showLocatorInstanceFunc(owInstance.Raftable)
        )
    itemProgressCanvas.MouseLeave.Add(fun _ -> hideLocator())

    // Version
    let vb = Graphics.makeButton(sprintf "v%s" OverworldData.VersionString, Some(12.), Some(Brushes.Orange))
    canvasAdd(appMainCanvas, vb, 0., THRU_MAP_AND_LEGEND_H + 4.)
    let mutable popupIsActive = false
    vb.Click.Add(fun _ ->
        if not popupIsActive then
            async {
                let! r = CustomComboBoxes.DoModalMessageBox(cm, System.Drawing.SystemIcons.Information, OverworldData.AboutBody, ["Go to website"; "Ok"])
                popupIsActive <- false
                if r = "Go to website" then
                    System.Diagnostics.Process.Start(OverworldData.Website) |> ignore
            } |> Async.StartImmediate
        )

    let HINTGRID_W, HINTGRID_H = 180., 36.
    let hintGrid = makeGrid(3,OverworldData.hintMeanings.Length,int HINTGRID_W,int HINTGRID_H)
    let mutable row=0 
    for a,b in OverworldData.hintMeanings do
        let thisRow = row
        gridAdd(hintGrid, new TextBox(FontSize=16., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(1.), Text=a), 0, row)
        let tb = new TextBox(FontSize=16., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(1.), Text=b)
        let dp = new DockPanel(LastChildFill=true)
        let bmp = 
            if row < 8 then
                Graphics.emptyUnfoundNumberedTriforce_bmps.[row]
            elif row = 8 then
                Graphics.unfoundL9_bmp
            elif row = 9 then
                Graphics.white_sword_bmp
            else
                Graphics.magical_sword_bmp
        let image = Graphics.BMPtoImage bmp
        image.Width <- 32.
        image.Stretch <- Stretch.None
        let b = new Border(Child=image, BorderThickness=Thickness(1.), BorderBrush=Brushes.LightGray, Background=Brushes.Black)
        DockPanel.SetDock(b, Dock.Left)
        dp.Children.Add(b) |> ignore
        dp.Children.Add(tb) |> ignore
        gridAdd(hintGrid, dp, 1, row)
        let mkTxt(text) = 
            new TextBox(FontSize=16., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, 
                        Width=HINTGRID_W-6., Height=HINTGRID_H-6., BorderThickness=Thickness(0.), VerticalAlignment=VerticalAlignment.Center, Text=text)
        let button = new Button(Content=mkTxt(TrackerModel.HintZone.FromIndex(0).ToString()))
        gridAdd(hintGrid, button, 2, row)
        let mutable popupIsActive = false
        let activatePopup(activationDelta) =
            popupIsActive <- true
            let tileX, tileY = (let p = button.TranslatePoint(Point(),appMainCanvas) in p.X+3., p.Y+3.)
            let tileCanvas = new Canvas(Width=HINTGRID_W-6., Height=HINTGRID_H-6., Background=Brushes.Black)
            let redrawTile(i) =
                tileCanvas.Children.Clear()
                canvasAdd(tileCanvas, mkTxt(TrackerModel.HintZone.FromIndex(i).ToString()), 3., 3.)
            let gridElementsSelectablesAndIDs = [|
                for i = 0 to 10 do
                    yield mkTxt(TrackerModel.HintZone.FromIndex(i).ToString()) :> FrameworkElement, true, i
                |]
            let originalStateIndex = TrackerModel.GetLevelHint(thisRow).ToIndex()
            let (gnc, gnr, gcw, grh) = 1, 11, int HINTGRID_W-6, int HINTGRID_H-6
            let gx,gy = HINTGRID_W-3., -HINTGRID_H*float(thisRow)-9.
            let onClick(_ea, i) = CustomComboBoxes.DismissPopupWithResult(i)
            let extraDecorations = []
            let brushes = CustomComboBoxes.ModalGridSelectBrushes.Defaults()
            let gridClickDismissalDoesMouseWarpBackToTileCenter = false
            async {
                let! r = CustomComboBoxes.DoModalGridSelect(cm, tileX, tileY, tileCanvas, gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (gnc, gnr, gcw, grh),
                                                gx, gy, redrawTile, onClick, extraDecorations, brushes, gridClickDismissalDoesMouseWarpBackToTileCenter)
                match r with
                | Some(i) ->
                    // update model
                    TrackerModel.SetLevelHint(thisRow, TrackerModel.HintZone.FromIndex(i))
                    TrackerModel.forceUpdate()
                    // update view
                    if i = 0 then
                        b.Background <- Brushes.Black
                    else
                        b.Background <- Views.hintHighlightBrush
                    button.Content <- mkTxt(TrackerModel.HintZone.FromIndex(i).ToString())
                | None -> ()
                popupIsActive <- false
                } |> Async.StartImmediate
        button.Click.Add(fun _ -> if not popupIsActive then activatePopup(0))
        button.MouseWheel.Add(fun x -> if not popupIsActive then activatePopup(if x.Delta>0 then -1 else 1))
        row <- row + 1
    let hintDescriptionTextBox = 
        new TextBox(FontSize=16., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.,0.,0.,4.), 
                    Text="Each hinted-but-not-yet-found location will cause a 'halo' to appear on\n"+
                         "the triforce/sword icon in the upper portion of the tracker, and hovering the\n"+
                         "halo will show the possible locations for that dungeon or sword cave.")
    let hintSP = new StackPanel(Orientation=Orientation.Vertical)
    hintSP.Children.Add(hintDescriptionTextBox) |> ignore
    hintSP.Children.Add(hintGrid) |> ignore
    let hintBorder = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(8.), Background=Brushes.Black, Child=hintSP)
    let tb = Graphics.makeButton("Hint Decoder", Some(12.), Some(Brushes.Orange))
    canvasAdd(appMainCanvas, tb, 530., THRU_MAP_AND_LEGEND_H + 6.)
    let mutable popupIsActive = false
    tb.Click.Add(fun _ -> 
        if not popupIsActive then
            popupIsActive <- true
            let wh = new System.Threading.ManualResetEvent(false)
            async {
                do! CustomComboBoxes.DoModal(cm, wh, 0., 65., hintBorder)
                popupIsActive <- false
                } |> Async.StartImmediate
        )

    // WANT!
    let kitty = new Image()
    let imageStream = Graphics.GetResourceStream("CroppedBrianKitty.png")
    kitty.Source <- System.Windows.Media.Imaging.BitmapFrame.Create(imageStream)
    kitty.Width <- THRU_MAIN_MAP_AND_ITEM_PROGRESS_H - THRU_MAIN_MAP_H
    kitty.Height <- THRU_MAIN_MAP_AND_ITEM_PROGRESS_H - THRU_MAIN_MAP_H
    canvasAdd(appMainCanvas, kitty, 16.*OMTW - kitty.Width, THRU_MAIN_MAP_H)

    // show hotkeys button
    let showHotKeysTB = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Graphics.almostBlack, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Show\nHotKeys", IsHitTestVisible=false)
    let showHotKeysButton = new Button(Content=showHotKeysTB)
    canvasAdd(appMainCanvas, showHotKeysButton, 16.*OMTW - kitty.Width - 60., THRU_MAIN_MAP_H)
    showHotKeysButton.Click.Add(fun _ ->
        let p = OverworldMapTileCustomization.MakeMappedHotKeysDisplay()
        let w = new Window()
        w.Title <- "Z-Tracker HotKeys"
        w.Owner <- Application.Current.MainWindow
        w.Content <- p
        p.Measure(Size(1280., 720.))
        w.Width <- p.DesiredSize.Width + 16.
        w.Height <- p.DesiredSize.Height + 40.
        w.SizeChanged.Add(fun _ -> refocusMainWindow())
        w.LocationChanged.Add(fun _ -> refocusMainWindow())
        w.Show()
        )

    let blockerDungeonSunglasses : FrameworkElement[] = Array.zeroCreate 8
    let mutable oneTimeRemindLadder, oneTimeRemindAnyKey = None, None
    doUIUpdateEvent.Publish.Add(fun () ->
        if displayIsCurrentlyMirrored <> TrackerModel.Options.Overworld.MirrorOverworld.Value then
            // model changed, align the view
            displayIsCurrentlyMirrored <- not displayIsCurrentlyMirrored
            if displayIsCurrentlyMirrored then
                for fe in mirrorOverworldFEs do
                    fe.RenderTransform <- new ScaleTransform(-1., 1., fe.ActualWidth/2., fe.ActualHeight/2.)
            else
                for fe in mirrorOverworldFEs do
                    fe.RenderTransform <- null
        // redraw triforce display (some may have located/unlocated/hinted)
        updateNumberedTriforceDisplayIfItExists()
        // redraw white/magical swords (may have located/unlocated/hinted)
        redrawWhiteSwordCanvas(white_sword_canvas)
        redrawMagicalSwordCanvas(mags_canvas)

        recorderingCanvas.Children.Clear()
        // TODO event for redraw item progress? does any of this event interface make sense? hmmm
        itemProgressCanvas.Children.Clear()
        let mutable x, y = ITEM_PROGRESS_FIRST_ITEM, 3.
        let DX = 30.
        canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.swordLevelToBmp(TrackerModel.playerComputedStateSummary.SwordLevel)), x, y)
        x <- x + DX
        match TrackerModel.playerComputedStateSummary.CandleLevel with
        | 0 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.red_candle_bmp), x, y)
        | 1 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.blue_candle_bmp, x, y)
        | 2 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.red_candle_bmp, x, y)
        | _ -> failwith "bad CandleLevel"
        x <- x + DX
        canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.ringLevelToBmp(TrackerModel.playerComputedStateSummary.RingLevel)), x, y)
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HaveBow then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.bow_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.bow_bmp), x, y)
        x <- x + DX
        match TrackerModel.playerComputedStateSummary.ArrowLevel with
        | 0 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.silver_arrow_bmp), x, y)
        | 1 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.wood_arrow_bmp, x, y)
        | 2 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.silver_arrow_bmp, x, y)
        | _ -> failwith "bad ArrowLevel"
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HaveWand then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.wand_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.wand_bmp), x, y)
        x <- x + DX
        if TrackerModel.IsCurrentlyBook() then
            // book seed
            if TrackerModel.playerComputedStateSummary.HaveBookOrShield then
                canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.book_bmp, x, y)
            else
                canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.book_bmp), x, y)
        else
            // boomstick seed
            if TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBoomBook.Value() then
                canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.boom_book_bmp, x, y)
            else
                canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.boom_book_bmp), x, y)
        x <- x + DX
        match TrackerModel.playerComputedStateSummary.BoomerangLevel with
        | 0 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.magic_boomerang_bmp), x, y)
        | 1 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.boomerang_bmp, x, y)
        | 2 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.magic_boomerang_bmp, x, y)
        | _ -> failwith "bad BoomerangLevel"
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HaveLadder then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.ladder_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.ladder_bmp), x, y)
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HaveRecorder then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.recorder_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.recorder_bmp), x, y)
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HavePowerBracelet then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.power_bracelet_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.power_bracelet_bmp), x, y)
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HaveRaft then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.raft_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.raft_bmp), x, y)
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HaveAnyKey then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.key_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.key_bmp), x, y)
        // place start icon in top layer
        if TrackerModel.startIconX <> -1 then
            canvasAdd(recorderingCanvas, startIcon, 11.5*OMTW/48.-3.+OMTW*float(TrackerModel.startIconX), float(TrackerModel.startIconY*11*3))
        TrackerModel.allUIEventingLogic( {new TrackerModel.ITrackerEvents with
            member _this.CurrentHearts(h) = currentMaxHeartsTextBox.Text <- sprintf "Max Hearts: %d" h
            member _this.AnnounceConsiderSword2() = 
                let n = TrackerModel.sword2Box.CellCurrent()
                if n = -1 then
                    SendReminder(TrackerModel.ReminderCategory.SwordHearts, "Consider getting the white sword item", 
                                    [upcb(Graphics.iconRightArrow_bmp); upcb(MapStateProxy(14).DefaultInteriorBmp())])
                else
                    SendReminder(TrackerModel.ReminderCategory.SwordHearts, sprintf "Consider getting the %s from the white sword cave" (TrackerModel.ITEMS.AsPronounceString(n)),
                                    [upcb(Graphics.iconRightArrow_bmp); upcb(MapStateProxy(14).DefaultInteriorBmp()); 
                                        upcb(CustomComboBoxes.boxCurrentBMP(TrackerModel.sword2Box.CellCurrent(), false))])
            member _this.AnnounceConsiderSword3() = SendReminder(TrackerModel.ReminderCategory.SwordHearts, "Consider the magical sword", [upcb(Graphics.iconRightArrow_bmp); upcb(Graphics.magical_sword_bmp)])
            member _this.OverworldSpotsRemaining(remain,gettable) = 
                owRemainingScreensTextBox.Text <- sprintf "%d OW spots left" remain
                owGettableScreensTextBox.Text <- sprintf "%d gettable" gettable
            member _this.DungeonLocation(i,x,y,hasTri,isCompleted) =
                if isCompleted then
                    drawCompletedDungeonHighlight(recorderingCanvas,float x,y)
                // highlight any triforce dungeons as recorder warp destinations
                if TrackerModel.playerComputedStateSummary.HaveRecorder && hasTri then
                    drawDungeonRecorderWarpHighlight(recorderingCanvas,float x,y)
                owUpdateFunctions.[x,y] 0 null  // redraw the tile, e.g. to recolor based on triforce-having
            member _this.AnyRoadLocation(i,x,y) = ()
            member _this.WhistleableLocation(x,y) = ()
            member _this.Armos(x,y) = 
                if TrackerModel.armosBox.PlayerHas() <> TrackerModel.PlayerHas.NO then
                    drawCompletedIconHighlight(recorderingCanvas,float x,y)  // darken a gotten armos icon
            member _this.Sword3(x,y) = 
                if TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Value() then
                    drawCompletedIconHighlight(recorderingCanvas,float x,y)  // darken a gotten magic sword cave icon
            member _this.Sword2(x,y) =
                owUpdateFunctions.[x,y] 0 null  // redraw the tile, e.g. to place/unplace the box and/or shift the icon
                if TrackerModel.sword2Box.PlayerHas() <> TrackerModel.PlayerHas.NO then
                    drawCompletedIconHighlight(recorderingCanvas,float x,y)  // darken a gotten white sword item cave icon
            member _this.RoutingInfo(haveLadder,haveRaft,currentRecorderWarpDestinations,currentAnyRoadDestinations,owRouteworthySpots) = 
                // clear and redraw routing
                routeDrawingCanvas.Children.Clear()
                OverworldRouting.repopulate(haveLadder,haveRaft,currentRecorderWarpDestinations,currentAnyRoadDestinations)
                let pos = System.Windows.Input.Mouse.GetPosition(routeDrawingCanvas)
                let i,j = int(Math.Floor(pos.X / OMTW)), int(Math.Floor(pos.Y / (11.*3.)))
                if i>=0 && i<16 && j>=0 && j<8 then
                    drawRoutesTo(currentRouteTarget(), routeDrawingCanvas,System.Windows.Point(0.,0.), i, j, TrackerModel.Options.Overworld.DrawRoutes.Value, if TrackerModel.Options.Overworld.HighlightNearby.Value then OverworldRouteDrawing.MaxYGH else 0)
                else
                    ensureRespectingOwGettableScreensCheckBox()
            member _this.AnnounceCompletedDungeon(i) = 
                let icons = [upcb(MapStateProxy(i).DefaultInteriorBmp()); upcb(Graphics.iconCheckMark_bmp)]
                if TrackerModel.IsHiddenDungeonNumbers() then
                    let labelChar = TrackerModel.GetDungeon(i).LabelChar
                    if labelChar <> '?' then
                        SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, sprintf "Dungeon %c is complete" labelChar, icons)
                    else
                        SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, "This dungeon is complete", icons)
                else
                    SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, sprintf "Dungeon %d is complete" (i+1), icons)
            member _this.CompletedDungeons(a) =
                for i = 0 to 7 do
                    // top ui
                    for j = 1 to 4 do
                        mainTrackerCanvases.[i,j].Children.Remove(mainTrackerCanvasShaders.[i,j]) |> ignore
                    if a.[i] then
                        for j = 1 to 4 do  // don't shade the color swatches
                            mainTrackerCanvases.[i,j].Children.Add(mainTrackerCanvasShaders.[i,j]) |> ignore
                    // blockers ui
                    if a.[i] then
                        blockerDungeonSunglasses.[i].Opacity <- 0.5
                    else
                        blockerDungeonSunglasses.[i].Opacity <- 1.
            member _this.AnnounceFoundDungeonCount(n) = 
                if DateTime.Now - mostRecentlyScrolledDungeonIndexTime.Time < TimeSpan.FromSeconds(1.5) then
                    selectDungeonTabEvent.Trigger(mostRecentlyScrolledDungeonIndex)
                let icons = [upcb(Graphics.genericDungeonInterior_bmp); ReminderTextBox(sprintf"%d/9"n)]
                if n = 1 then
                    SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, "You have located one dungeon", icons) 
                elif n = 9 then
                    SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, "Congratulations, you have located all 9 dungeons", [yield! icons; yield upcb(Graphics.iconCheckMark_bmp)])
                else
                    SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, sprintf "You have located %d dungeons" n, icons) 
            member _this.AnnounceTriforceCount(n) = 
                let icons = [upcb(Graphics.fullOrangeTriforce_bmp); ReminderTextBox(sprintf"%d/8"n)]
                if n = 1 then
                    SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, "You now have one triforce", icons)
                else
                    SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, sprintf "You now have %d triforces" n, [yield! icons; if n=8 then yield upcb(Graphics.iconCheckMark_bmp)])
                if n = 8 && not(TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Value()) then
                    SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, "Consider the magical sword before dungeon nine", [upcb(Graphics.iconRightArrow_bmp); upcb(Graphics.magical_sword_bmp)])
            member _this.AnnounceTriforceAndGo(triforceCount, tagSummary) = 
                let needSomeThingsicons = [
                    for _i = 1 to tagSummary.MissingDungeonCount do
                        yield upcb(Graphics.greyscale Graphics.genericDungeonInterior_bmp)
                    if not tagSummary.HaveBow then
                        yield upcb(Graphics.greyscale Graphics.bow_bmp)
                    if not tagSummary.HaveSilvers then
                        yield upcb(Graphics.greyscale Graphics.silver_arrow_bmp)
                    ]
                let triforceAndGoIcons = [
                    if triforceCount<>8 then
                        if needSomeThingsicons.Length<>0 then
                            yield upcb(Graphics.iconRightArrow_bmp)
                        for _i = 1 to (8-triforceCount) do
                            yield upcb(Graphics.greyTriforce_bmp)
                    yield upcb(Graphics.iconRightArrow_bmp)
                    yield upcb(Graphics.ganon_bmp)
                    ]
                let icons = [yield! needSomeThingsicons; yield! triforceAndGoIcons]
                let go = if triforceCount=8 then "go time" else "triforce and go"
                match tagSummary.Level with
                | 101 -> SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, "You might be "+go, icons)
                | 102 -> SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, "You are probably "+go, icons)
                | 103 -> SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, "You are "+go, icons)
                | 0 -> ()
                | _ -> SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, (sprintf "You need %s to be " (if needSomeThingsicons.Length>1 then "some things" else "something"))+go, icons)
            member _this.RemindUnblock(blockerType, dungeons, detail) =
                let name(d) =
                    if TrackerModel.IsHiddenDungeonNumbers() then
                        (char(int 'A' + d)).ToString()
                    else
                        (1+d).ToString()
                let icons = ResizeArray()
                let mutable sentence = "Now that you have"
                match blockerType with
                | TrackerModel.DungeonBlocker.COMBAT ->
                    let words = ResizeArray()
                    for d in detail do
                        match d with
                        | TrackerModel.CombatUnblockerDetail.BETTER_SWORD -> words.Add(" a better sword,"); icons.Add(upcb(Graphics.swordLevelToBmp(TrackerModel.playerComputedStateSummary.SwordLevel)))
                        | TrackerModel.CombatUnblockerDetail.BETTER_ARMOR -> words.Add(" better armor,"); icons.Add(upcb(Graphics.ringLevelToBmp(TrackerModel.playerComputedStateSummary.RingLevel)))
                        | TrackerModel.CombatUnblockerDetail.WAND -> words.Add(" the wand,"); icons.Add(upcb(Graphics.wand_bmp))
                    sentence <- sentence + System.String.Concat words
                | TrackerModel.DungeonBlocker.BOW_AND_ARROW -> sentence <- sentence + " a beau and arrow,"; icons.Add(upcb(Graphics.bow_and_arrow_bmp))
                | TrackerModel.DungeonBlocker.RECORDER -> sentence <- sentence + " the recorder,"; icons.Add(upcb(Graphics.recorder_bmp))
                | TrackerModel.DungeonBlocker.LADDER -> sentence <- sentence + " the ladder,"; icons.Add(upcb(Graphics.ladder_bmp))
                | TrackerModel.DungeonBlocker.KEY -> sentence <- sentence + " the any key,"; icons.Add(upcb(Graphics.key_bmp))
                | TrackerModel.DungeonBlocker.BOMB -> sentence <- sentence + " bombs,"; icons.Add(upcb(Graphics.bomb_bmp))
                | _ -> ()
                sentence <- sentence + " consider dungeon" + (if Seq.length dungeons > 1 then "s " else " ")
                icons.Add(upcb(Graphics.iconRightArrow_bmp))
                let d = Seq.head dungeons
                sentence <- sentence + name(d)
                icons.Add(upcb(MapStateProxy(d).DefaultInteriorBmp()))
                for d in Seq.tail dungeons do
                    sentence <- sentence + " and " + name(d)
                    icons.Add(upcb(MapStateProxy(d).DefaultInteriorBmp()))
                SendReminder(TrackerModel.ReminderCategory.Blockers, sentence, icons)
            member _this.RemindShortly(itemId) = 
                if itemId = TrackerModel.ITEMS.KEY then
                    oneTimeRemindAnyKey <- Some(new TrackerModel.LastChangedTime(), (fun() ->
                        oneTimeRemindAnyKey <- None
                        if TrackerModel.playerComputedStateSummary.HaveAnyKey then
                            SendReminder(TrackerModel.ReminderCategory.HaveKeyLadder, "Don't forget that you have the any key", [upcb(Graphics.key_bmp)])
                        else
                            TrackerModel.remindedAnyKey <- false))
                elif itemId = TrackerModel.ITEMS.LADDER then
                    oneTimeRemindLadder <- Some(new TrackerModel.LastChangedTime(), (fun() ->
                        oneTimeRemindLadder <- None
                        if TrackerModel.playerComputedStateSummary.HaveLadder then
                            SendReminder(TrackerModel.ReminderCategory.HaveKeyLadder, "Don't forget that you have the ladder", [upcb(Graphics.ladder_bmp)])
                        else
                            TrackerModel.remindedLadder <- false))
                else
                    failwith "bad reminder"
            })
        )
    let threshold = TimeSpan.FromMilliseconds(500.0)
    let recentlyAgo = TimeSpan.FromMinutes(3.0)
    let ladderTime, recorderTime, powerBraceletTime, boomstickTime = 
        new TrackerModel.LastChangedTime(recentlyAgo), new TrackerModel.LastChangedTime(recentlyAgo), new TrackerModel.LastChangedTime(recentlyAgo), new TrackerModel.LastChangedTime(recentlyAgo)
    let mutable owPreviouslyAnnouncedWhistleSpotsRemain, owPreviouslyAnnouncedPowerBraceletSpotsRemain = 0, 0
    let timer = new System.Windows.Threading.DispatcherTimer()
    timer.Interval <- TimeSpan.FromSeconds(1.0)
    timer.Tick.Add(fun _ -> 
        if not(TrackerModel.LastChangedTime.IsPaused) then
            let hasUISettledDown = 
                DateTime.Now - TrackerModel.playerProgressLastChangedTime.Time > threshold &&
                DateTime.Now - TrackerModel.dungeonsAndBoxesLastChangedTime.Time > threshold &&
                DateTime.Now - TrackerModel.mapLastChangedTime.Time > threshold
            if hasUISettledDown then
                let hasTheModelChanged = TrackerModel.recomputeWhatIsNeeded()  
                if hasTheModelChanged then
                    doUIUpdateEvent.Trigger()
            // link animation
            stepAnimateLink()
            // remind ladder
            if (DateTime.Now - ladderTime.Time).Minutes > 2 then  // every 3 mins
                if TrackerModel.playerComputedStateSummary.HaveLadder then
                    if not(TrackerModel.playerComputedStateSummary.HaveCoastItem) then
                        let n = TrackerModel.ladderBox.CellCurrent()
                        if n = -1 then
                            SendReminder(TrackerModel.ReminderCategory.CoastItem, "Get the coast item with the ladder", [upcb(Graphics.ladder_bmp); upcb(Graphics.iconRightArrow_bmp)])
                        else
                            SendReminder(TrackerModel.ReminderCategory.CoastItem, sprintf "Get the %s off the coast" (TrackerModel.ITEMS.AsPronounceString(n)),
                                            [upcb(Graphics.ladder_bmp); upcb(Graphics.iconRightArrow_bmp); upcb(CustomComboBoxes.boxCurrentBMP(TrackerModel.ladderBox.CellCurrent(), false))])
                        ladderTime.SetNow()
            // remind whistle spots
            if (DateTime.Now - recorderTime.Time).Minutes > 2 then  // every 3 mins
                if TrackerModel.playerComputedStateSummary.HaveRecorder then
                    let owWhistleSpotsRemain = TrackerModel.mapStateSummary.OwWhistleSpotsRemain.Count
                    if owWhistleSpotsRemain >= owPreviouslyAnnouncedWhistleSpotsRemain && owWhistleSpotsRemain > 0 then
                        let icons = [upcb(Graphics.recorder_bmp); ReminderTextBox(owWhistleSpotsRemain.ToString())]
                        if owWhistleSpotsRemain = 1 then
                            SendReminder(TrackerModel.ReminderCategory.RecorderPBSpotsAndBoomstickBook, "There is one recorder spot", icons)
                        else
                            SendReminder(TrackerModel.ReminderCategory.RecorderPBSpotsAndBoomstickBook, sprintf "There are %d recorder spots" owWhistleSpotsRemain, icons)
                    recorderTime.SetNow()
                    owPreviouslyAnnouncedWhistleSpotsRemain <- owWhistleSpotsRemain
            // remind power bracelet spots
            if (DateTime.Now - powerBraceletTime.Time).Minutes > 2 then  // every 3 mins
                if TrackerModel.playerComputedStateSummary.HavePowerBracelet then
                    if TrackerModel.mapStateSummary.OwPowerBraceletSpotsRemain >= owPreviouslyAnnouncedPowerBraceletSpotsRemain && TrackerModel.mapStateSummary.OwPowerBraceletSpotsRemain > 0 then
                        let icons = [upcb(Graphics.power_bracelet_bmp); ReminderTextBox(TrackerModel.mapStateSummary.OwPowerBraceletSpotsRemain.ToString())]
                        if TrackerModel.mapStateSummary.OwPowerBraceletSpotsRemain = 1 then
                            SendReminder(TrackerModel.ReminderCategory.RecorderPBSpotsAndBoomstickBook, "There is one power bracelet spot", icons)
                        else
                            SendReminder(TrackerModel.ReminderCategory.RecorderPBSpotsAndBoomstickBook, sprintf "There are %d power bracelet spots" TrackerModel.mapStateSummary.OwPowerBraceletSpotsRemain, icons)
                    powerBraceletTime.SetNow()
                    owPreviouslyAnnouncedPowerBraceletSpotsRemain <- TrackerModel.mapStateSummary.OwPowerBraceletSpotsRemain
            // remind boomstick book
            if (DateTime.Now - boomstickTime.Time).Minutes > 2 then  // every 3 mins
                if TrackerModel.playerComputedStateSummary.HaveWand && not(TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBoomBook.Value()) then
                    if TrackerModel.mapStateSummary.BoomBookShopLocation<>TrackerModel.NOTFOUND then
                        SendReminder(TrackerModel.ReminderCategory.RecorderPBSpotsAndBoomstickBook, "Consider buying the boomstick book", [upcb(Graphics.iconRightArrow_bmp); upcb(Graphics.boom_book_bmp)])
                        boomstickTime.SetNow()
            // one-time reminders
            match oneTimeRemindAnyKey with
            | None -> ()
            | Some(lct, thunk) ->
                if (DateTime.Now - lct.Time).Minutes > 0 then  // 1 min
                    thunk()
            match oneTimeRemindLadder with
            | None -> ()
            | Some(lct, thunk) ->
                if (DateTime.Now - lct.Time).Minutes > 0 then  // 1 min
                    thunk()
        )
    timer.Start()

    // Dungeon level trackers
    let rightwardCanvas = new Canvas()
    let dungeonTabs,grabModeTextBlock = 
        DungeonUI.makeDungeonTabs(cm, START_DUNGEON_AND_NOTES_AREA_H, selectDungeonTabEvent, TH, rightwardCanvas, (fun level ->
            let i,j = TrackerModel.mapStateSummary.DungeonLocations.[level-1]
            if (i,j) <> TrackerModel.NOTFOUND then
                // when mouse in a dungeon map, show its location...
                showLocatorExactLocation(TrackerModel.mapStateSummary.DungeonLocations.[level-1])
                // ...and behave like we are moused there
                drawRoutesTo(None, routeDrawingCanvas, Point(), i, j, TrackerModel.Options.Overworld.DrawRoutes.Value, 
                                    if TrackerModel.Options.Overworld.HighlightNearby.Value then OverworldRouteDrawing.MaxYGH else 0)
            ), (fun _level -> hideLocator()))
    canvasAdd(appMainCanvas, dungeonTabs, 0., START_DUNGEON_AND_NOTES_AREA_H)
    canvasAdd(appMainCanvas, dungeonTabsOverlay, 0., START_DUNGEON_AND_NOTES_AREA_H+float(TH))

    let BLOCKERS_AND_NOTES_OFFSET = 408. + 42.  // dungeon area and side-tracker-panel
    // blockers
    let makeBlockerBox(dungeonIndex, blockerIndex) =
        let make() =
            let c = new Canvas(Width=30., Height=30., Background=Brushes.Black, IsHitTestVisible=true)
            let rect = new Shapes.Rectangle(Width=30., Height=30., Stroke=Brushes.Gray, StrokeThickness=3.0, IsHitTestVisible=false)
            c.Children.Add(rect) |> ignore
            let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Transparent, IsHitTestVisible=false)  // just has item drawn on it, not the box
            c.Children.Add(innerc) |> ignore
            let redraw(n) =
                innerc.Children.Clear()
                let bmp = Graphics.blockerCurrentBMP(n)
                if bmp <> null then
                    let image = Graphics.BMPtoImage(bmp)
                    image.IsHitTestVisible <- false
                    canvasAdd(innerc, image, 4., 4.)
                innerc
            c, redraw
        let c,redraw = make()
        let mutable current = TrackerModel.DungeonBlocker.NOTHING
        redraw(current) |> ignore
        let mutable popupIsActive = false
        let SetNewValue(db) =
            current <- db
            redraw(db) |> ignore
            TrackerModel.dungeonBlockers.[dungeonIndex, blockerIndex] <- db
        let activate(activationDelta) =
            popupIsActive <- true
            let pc, predraw = make()
            let popupRedraw(n) =
                let innerc = predraw(n)
                let text = new TextBox(Text=n.DisplayDescription(), Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.),
                                            FontSize=16., HorizontalContentAlignment=HorizontalAlignment.Center)
                let textBorder = new Border(BorderThickness=Thickness(3.), Child=text, Background=Brushes.Black, BorderBrush=Brushes.Gray)
                let dp = new DockPanel(LastChildFill=false)
                DockPanel.SetDock(textBorder, Dock.Right)
                dp.Children.Add(textBorder) |> ignore
                Canvas.SetTop(dp, 30.)
                Canvas.SetRight(dp, 90.)
                innerc.Children.Add(dp) |> ignore
            let pos = c.TranslatePoint(Point(), appMainCanvas)
            async {
                let! r = CustomComboBoxes.DoModalGridSelect(cm, pos.X, pos.Y, pc, TrackerModel.DungeonBlocker.All |> Array.map (fun db ->
                                (if db=TrackerModel.DungeonBlocker.NOTHING then upcast Canvas() else upcast Graphics.BMPtoImage(Graphics.blockerCurrentBMP(db))), true, db), 
                                Array.IndexOf(TrackerModel.DungeonBlocker.All, current), activationDelta, (3, 3, 21, 21), -60., 30., popupRedraw,
                                (fun (_ea,db) -> CustomComboBoxes.DismissPopupWithResult(db)), [], CustomComboBoxes.ModalGridSelectBrushes.Defaults(), true)
                match r with
                | Some(db) -> SetNewValue(db)
                | None -> () 
                popupIsActive <- false
                } |> Async.StartImmediate
        c.MouseWheel.Add(fun x -> if not popupIsActive then activate(if x.Delta<0 then 1 else -1))
        c.MouseDown.Add(fun _ -> if not popupIsActive then activate(0))
        c.MyKeyAdd(fun ea -> 
            if not popupIsActive then
                match HotKeys.BlockerHotKeyProcessor.TryGetValue(ea.Key) with
                | Some(db) -> 
                    ea.Handled <- true
                    if current = db then
                        SetNewValue(TrackerModel.DungeonBlocker.NOTHING)    // idempotent hotkeys behave as a toggle
                    else
                        SetNewValue(db)
                | None -> ()
            )
        c

    let blockerColumnWidth = int((appMainCanvas.Width-BLOCKERS_AND_NOTES_OFFSET)/3.)
    let blockerGrid = makeGrid(3, 3, blockerColumnWidth, 36)
    blockerGrid.Height <- float(36*3)
    for i = 0 to 2 do
        for j = 0 to 2 do
            if i=0 && j=0 then
                let d = new DockPanel(LastChildFill=false, Background=Brushes.Black)
                let tb = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=12., Text="BLOCKERS", Width=float blockerColumnWidth, IsHitTestVisible=false,
                                        VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), TextAlignment=TextAlignment.Center)
                d.ToolTip <- "The icons you set in this area can remind you of what blocked you in a dungeon.\nFor example, a ladder represents being ladder blocked, or a sword means you need better weapons.\nSome reminders will trigger when you get the item that may unblock you."
                d.Children.Add(tb) |> ignore
                gridAdd(blockerGrid, d, i, j)
            else
                let dungeonIndex = (3*j+i)-1
                let labelChar = if TrackerModel.IsHiddenDungeonNumbers() then "ABCDEFGH".[dungeonIndex] else "12345678".[dungeonIndex]
                let d = new DockPanel(LastChildFill=false)
                let sp = new StackPanel(Orientation=Orientation.Horizontal)
                let tb = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=12., Text=sprintf "%c" labelChar, Width=30., IsHitTestVisible=false,
                                        VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), 
                                        TextAlignment=TextAlignment.Right, Margin=Thickness(0.,0.,6.,0.))
                sp.Children.Add(tb) |> ignore
                sp.Children.Add(makeBlockerBox(dungeonIndex, 0)) |> ignore
                sp.Children.Add(makeBlockerBox(dungeonIndex, 1)) |> ignore
                d.Children.Add(sp) |> ignore
                gridAdd(blockerGrid, d, i, j)
                blockerDungeonSunglasses.[dungeonIndex] <- upcast sp // just reduce its opacity
    canvasAdd(appMainCanvas, blockerGrid, BLOCKERS_AND_NOTES_OFFSET, START_DUNGEON_AND_NOTES_AREA_H) 

    // notes    
    let tb = new TextBox(Width=appMainCanvas.Width-BLOCKERS_AND_NOTES_OFFSET, Height=dungeonTabs.Height - blockerGrid.Height)
    notesTextBox <- tb
    tb.FontSize <- 24.
    tb.Foreground <- System.Windows.Media.Brushes.LimeGreen 
    tb.Background <- System.Windows.Media.Brushes.Black 
    tb.Text <- "Notes\n"
    tb.AcceptsReturn <- true
    canvasAdd(appMainCanvas, tb, BLOCKERS_AND_NOTES_OFFSET, START_DUNGEON_AND_NOTES_AREA_H + blockerGrid.Height) 

    grabModeTextBlock.Opacity <- 0.
    grabModeTextBlock.Width <- tb.Width
    canvasAdd(appMainCanvas, grabModeTextBlock, BLOCKERS_AND_NOTES_OFFSET, START_DUNGEON_AND_NOTES_AREA_H) 

    canvasAdd(appMainCanvas, rightwardCanvas, BLOCKERS_AND_NOTES_OFFSET, START_DUNGEON_AND_NOTES_AREA_H)  // extra place for dungeonTabs to draw atop blockers/notes

    // remaining OW spots
    canvasAdd(appMainCanvas, owRemainingScreensTextBox, RIGHT_COL, 90.)
    owRemainingScreensTextBox.MouseEnter.Add(fun _ ->
        let unmarked = TrackerModel.overworldMapMarks |> Array2D.map (fun cell -> cell.Current() = -1)
        OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, unmarked, 
                                            unmarked, Point(0.,0.), 0, 0, false, true, 128)
        )
    owRemainingScreensTextBox.MouseLeave.Add(fun _ ->
        routeDrawingCanvas.Children.Clear()
        ensureRespectingOwGettableScreensCheckBox()
        )
    canvasAdd(appMainCanvas, owGettableScreensCheckBox, RIGHT_COL, 110.)
    owGettableScreensCheckBox.Checked.Add(fun _ -> TrackerModel.forceUpdate()) 
    owGettableScreensCheckBox.Unchecked.Add(fun _ -> TrackerModel.forceUpdate())
    owGettableScreensTextBox.MouseEnter.Add(fun _ -> 
        OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, TrackerModel.mapStateSummary.OwRouteworthySpots, 
                                            TrackerModel.overworldMapMarks |> Array2D.map (fun cell -> cell.Current() = -1), Point(0.,0.), 0, 0, false, true, 128)
        )
    owGettableScreensTextBox.MouseLeave.Add(fun _ -> 
        if not(owGettableScreensCheckBox.IsChecked.HasValue) || not(owGettableScreensCheckBox.IsChecked.Value) then 
            routeDrawingCanvas.Children.Clear()
        )
    // current max hearts
    canvasAdd(appMainCanvas, currentMaxHeartsTextBox, RIGHT_COL, 130.)
    // coordinate grid
    let owCoordsGrid = makeGrid(16, 8, int OMTW, 11*3)
    let owCoordsTBs = Array2D.zeroCreate 16 8
    for i = 0 to 15 do
        for j = 0 to 7 do
            let tb = new TextBox(Text=sprintf "%c  %d" (char (int 'A' + j)) (i+1),  // may change with OMTW and overall layout
                                    Foreground=Brushes.White, Background=Brushes.Transparent, BorderThickness=Thickness(0.0), 
                                    FontFamily=FontFamily("Consolas"), FontSize=16.0, FontWeight=FontWeights.Bold, IsHitTestVisible=false)
            mirrorOverworldFEs.Add(tb)
            tb.Opacity <- 0.0
            tb.IsHitTestVisible <- false // transparent to mouse
            owCoordsTBs.[i,j] <- tb
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            canvasAdd(c, tb, 2., 6.)
            gridAdd(owCoordsGrid, c, i, j) 
    canvasAdd(overworldCanvas, owCoordsGrid, 0., 0.)
    let showCoords = new TextBox(Text="Coords",FontSize=14.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true)
    let cb = new CheckBox(Content=showCoords)
    cb.IsChecked <- System.Nullable.op_Implicit false
    cb.Checked.Add(fun _ -> owCoordsTBs |> Array2D.iter (fun i -> i.Opacity <- 0.85))
    cb.Unchecked.Add(fun _ -> owCoordsTBs |> Array2D.iter (fun i -> i.Opacity <- 0.0))
    showCoords.MouseEnter.Add(fun _ -> if not cb.IsChecked.HasValue || not cb.IsChecked.Value then owCoordsTBs |> Array2D.iter (fun i -> i.Opacity <- 0.85))
    showCoords.MouseLeave.Add(fun _ -> if not cb.IsChecked.HasValue || not cb.IsChecked.Value then owCoordsTBs |> Array2D.iter (fun i -> i.Opacity <- 0.0))
    canvasAdd(appMainCanvas, cb, OW_ITEM_GRID_LOCATIONS.OFFSET+200., 72.)

    // zone overlay
    let owMapZoneColorCanvases, owMapZoneBlackCanvases =
        let avg(c1:System.Drawing.Color, c2:System.Drawing.Color) = System.Drawing.Color.FromArgb((int c1.R + int c2.R)/2, (int c1.G + int c2.G)/2, (int c1.B + int c2.B)/2)
        let toBrush(c:System.Drawing.Color) = new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B))
        let colors = 
            dict [
                'M', avg(System.Drawing.Color.Pink, System.Drawing.Color.Crimson) |> toBrush
                'L', System.Drawing.Color.BlueViolet |> toBrush
                'R', System.Drawing.Color.LightSeaGreen |> toBrush
                'H', System.Drawing.Color.Gray |> toBrush
                'C', System.Drawing.Color.LightBlue |> toBrush
                'G', avg(System.Drawing.Color.LightSteelBlue, System.Drawing.Color.SteelBlue) |> toBrush
                'D', System.Drawing.Color.Orange |> toBrush
                'F', System.Drawing.Color.LightGreen |> toBrush
                'S', System.Drawing.Color.DarkGray |> toBrush
                'W', System.Drawing.Color.Brown |> toBrush
            ]
        let imgs,darks = Array2D.zeroCreate 16 8, Array2D.zeroCreate 16 8
        for x = 0 to 15 do
            for y = 0 to 7 do
                imgs.[x,y] <- new Canvas(Width=OMTW, Height=float(11*3), Background=colors.Item(OverworldData.owMapZone.[y].[x]), IsHitTestVisible=false)
                darks.[x,y] <- new Canvas(Width=OMTW, Height=float(11*3), Background=Brushes.Black, IsHitTestVisible=false)
        imgs, darks
    let owMapZoneGrid = makeGrid(16, 8, int OMTW, 11*3)
    let allOwMapZoneColorCanvases,allOwMapZoneBlackCanvases = Array2D.zeroCreate 16 8, Array2D.zeroCreate 16 8
    for i = 0 to 15 do
        for j = 0 to 7 do
            let zcc,zbc = owMapZoneColorCanvases.[i,j], owMapZoneBlackCanvases.[i,j]
            zcc.Opacity <- 0.0
            zbc.Opacity <- 0.0
            allOwMapZoneColorCanvases.[i,j] <- zcc
            allOwMapZoneBlackCanvases.[i,j] <- zbc
            gridAdd(owMapZoneGrid, zcc, i, j)
            gridAdd(owMapZoneGrid, zbc, i, j)
    canvasAdd(overworldCanvas, owMapZoneGrid, 0., 0.)

    let owMapZoneBoundaries = ResizeArray()
    let makeLine(x1, x2, y1, y2) = 
        let line = new System.Windows.Shapes.Line(X1=OMTW*float(x1), X2=OMTW*float(x2), Y1=float(y1*11*3), Y2=float(y2*11*3), Stroke=Brushes.White, StrokeThickness=3.)
        line.IsHitTestVisible <- false // transparent to mouse
        line
    let addLine(x1,x2,y1,y2) = 
        let line = makeLine(x1,x2,y1,y2)
        line.Opacity <- 0.0
        owMapZoneBoundaries.Add(line)
        canvasAdd(overworldCanvas, line, 0., 0.)
    addLine(0,7,2,2)
    addLine(7,11,1,1)
    addLine(7,7,1,2)
    addLine(10,10,0,1)
    addLine(11,11,0,2)
    addLine(8,14,2,2)
    addLine(14,14,0,2)
    addLine(6,6,2,3)
    addLine(4,4,3,4)
    addLine(2,2,4,5)
    addLine(1,1,5,7)
    addLine(0,1,7,7)
    addLine(1,4,5,5)
    addLine(2,4,4,4)
    addLine(4,6,3,3)
    addLine(4,7,6,6)
    addLine(7,12,5,5)
    addLine(9,10,4,4)
    addLine(7,10,3,3)
    addLine(7,7,2,3)
    addLine(10,10,3,4)
    addLine(9,9,4,7)
    addLine(7,7,5,6)
    addLine(4,4,5,6)
    addLine(5,5,6,8)
    addLine(6,6,6,8)
    addLine(11,11,5,8)
    addLine(9,15,7,7)
    addLine(12,12,3,5)
    addLine(13,13,2,3)
    addLine(8,8,2,3)
    addLine(12,14,3,3)
    addLine(14,15,4,4)
    addLine(15,15,4,7)
    addLine(14,14,3,4)

    let zoneNames = ResizeArray()  // added later, to be top of z-order
    let addZoneName(hz, name, x, y) =
        let tb = new TextBox(Text=name,FontSize=16.,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(2.),IsReadOnly=true)
        mirrorOverworldFEs.Add(tb)
        canvasAdd(overworldCanvas, tb, x*OMTW, y*11.*3.)
        tb.Opacity <- 0.
        tb.TextAlignment <- TextAlignment.Center
        tb.FontWeight <- FontWeights.Bold
        tb.IsHitTestVisible <- false
        zoneNames.Add(hz,tb)

    let changeZoneOpacity(hintZone,show) =
        let noZone = hintZone=TrackerModel.HintZone.UNKNOWN
        if show then
            if noZone then 
                allOwMapZoneColorCanvases |> Array2D.iteri (fun _x _y zcc -> zcc.Opacity <- 0.3)
            owMapZoneBoundaries |> Seq.iter (fun x -> x.Opacity <- 0.9)
            zoneNames |> Seq.iter (fun (hz,textbox) -> if noZone || hz=hintZone then textbox.Opacity <- 0.6)
        else
            allOwMapZoneColorCanvases |> Array2D.iteri (fun _x _y zcc -> zcc.Opacity <- 0.0)
            owMapZoneBoundaries |> Seq.iter (fun x -> x.Opacity <- 0.0)
            zoneNames |> Seq.iter (fun (_hz,textbox) -> textbox.Opacity <- 0.0)
    let zone_checkbox = new CheckBox(Content=new TextBox(Text="Zones",FontSize=14.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    zone_checkbox.IsChecked <- System.Nullable.op_Implicit false
    zone_checkbox.Checked.Add(fun _ -> changeZoneOpacity(TrackerModel.HintZone.UNKNOWN,true))
    zone_checkbox.Unchecked.Add(fun _ -> changeZoneOpacity(TrackerModel.HintZone.UNKNOWN,false))
    zone_checkbox.MouseEnter.Add(fun _ -> if not zone_checkbox.IsChecked.HasValue || not zone_checkbox.IsChecked.Value then changeZoneOpacity(TrackerModel.HintZone.UNKNOWN,true))
    zone_checkbox.MouseLeave.Add(fun _ -> if not zone_checkbox.IsChecked.HasValue || not zone_checkbox.IsChecked.Value then changeZoneOpacity(TrackerModel.HintZone.UNKNOWN,false))
    canvasAdd(appMainCanvas, zone_checkbox, OW_ITEM_GRID_LOCATIONS.OFFSET+200., 52.)

    let owLocatorGrid = makeGrid(16, 8, int OMTW, 11*3)
    let owLocatorTilesZone = Array2D.zeroCreate 16 8
    let owLocatorCanvas = new Canvas()

    for i = 0 to 15 do
        for j = 0 to 7 do
            let z = new Graphics.TileHighlightRectangle()
            z.Hide()
            owLocatorTilesZone.[i,j] <- z
            for s in z.Shapes do
                gridAdd(owLocatorGrid, s, i, j)
    canvasAdd(overworldCanvas, owLocatorGrid, 0., 0.)
    canvasAdd(overworldCanvas, owLocatorCanvas, 0., 0.)

    showLocatorExactLocation <- (fun (x,y) ->
        if (x,y) <> TrackerModel.NOTFOUND then
            let leftLine = new Shapes.Line(X1=OMTW*float x, Y1=0., X2=OMTW*float x, Y2=float(8*11*3), Stroke=Brushes.White, StrokeThickness=2., IsHitTestVisible=false)
            canvasAdd(owLocatorCanvas, leftLine, 0., 0.)
            let rightLine = new Shapes.Line(X1=OMTW*float (x+1)-1., Y1=0., X2=OMTW*float (x+1)-1., Y2=float(8*11*3), Stroke=Brushes.White, StrokeThickness=2., IsHitTestVisible=false)
            canvasAdd(owLocatorCanvas, rightLine, 0., 0.)
            let topLine = new Shapes.Line(X1=0., Y1=float(y*11*3), X2=OMTW*float(16*3), Y2=float(y*11*3), Stroke=Brushes.White, StrokeThickness=2., IsHitTestVisible=false)
            canvasAdd(owLocatorCanvas, topLine, 0., 0.)
            let bottomLine = new Shapes.Line(X1=0., Y1=float((y+1)*11*3)-1., X2=OMTW*float(16*3), Y2=float((y+1)*11*3)-1., Stroke=Brushes.White, StrokeThickness=2., IsHitTestVisible=false)
            canvasAdd(owLocatorCanvas, bottomLine, 0., 0.)
        )
    showLocatorHintedZone <- (fun (hinted_zone, alsoHighlightABCDEFGH) ->
        routeDrawingCanvas.Children.Clear()
        if hinted_zone <> TrackerModel.HintZone.UNKNOWN then
            // have hint, so draw that zone...
            let inZone = Array2D.init 16 8 (fun x y -> OverworldData.owMapZone.[y].[x] = hinted_zone.AsDataChar())
            for x = 0 to 15 do
                for y = 0 to 7 do
                    if not(inZone.[x,y]) then
                        allOwMapZoneBlackCanvases.[x,y].Opacity <- 0.7
            let isZone(x,y) = inZone.[max 0 (min 15 x), max 0 (min 7 y)]  // if x or y out of bounds, grab nearby in-bounds value
            let ST, OFF, COLOR = 3., 3./2., Brushes.LightGray
            for x = 0 to 15 do
                for y = 0 to 7 do
                    let mkLine(x1,y1,x2,y2) = 
                        let line = new Shapes.Line(X1=x1, Y1=y1, X2=x2, Y2=y2, Stroke=COLOR, StrokeThickness=ST, IsHitTestVisible=false)
                        line.StrokeDashArray <- new DoubleCollection( seq[1.; 1.5] )
                        line
                    if isZone(x,y) && not(isZone(x-1,y)) then  // left
                        let line = mkLine(OMTW*float(x)-OFF, float(y*11*3), OMTW*float(x)-OFF, float((y+1)*11*3))
                        canvasAdd(owLocatorCanvas, line, 0., 0.)
                    if isZone(x,y) && not(isZone(x+1,y)) then  // right
                        let line = mkLine(OMTW*float(x+1)+OFF, float(y*11*3), OMTW*float(x+1)+OFF, float((y+1)*11*3))
                        canvasAdd(owLocatorCanvas, line, 0., 0.)
                    if isZone(x,y) && not(isZone(x,y-1)) then  // top
                        let line = mkLine(OMTW*float(x), float(y*11*3)-OFF, OMTW*float(x+1), float(y*11*3)-OFF)
                        canvasAdd(owLocatorCanvas, line, 0., 0.)
                    if isZone(x,y) && not(isZone(x,y+1)) then  // bottom
                        let line = mkLine(OMTW*float(x), float((y+1)*11*3)+OFF, OMTW*float(x+1), float((y+1)*11*3)+OFF)
                        canvasAdd(owLocatorCanvas, line, 0., 0.)
            for i = 0 to 15 do
                for j = 0 to 7 do
                    // ... and highlight all undiscovered tiles
                    if OverworldData.owMapZone.[j].[i] = hinted_zone.AsDataChar() then
                        let cur = TrackerModel.overworldMapMarks.[i,j].Current()
                        let isLetteredNumberlessDungeon = (alsoHighlightABCDEFGH && cur>=0 && cur<=7 && TrackerModel.GetDungeon(cur).LabelChar='?')
                        if cur = -1 || isLetteredNumberlessDungeon then
                            if TrackerModel.mapStateSummary.OwGettableLocations.Contains(i,j) then
                                if owInstance.SometimesEmpty(i,j) then
                                    owLocatorTilesZone.[i,j].MakeYellow()
                                else
                                    owLocatorTilesZone.[i,j].MakeGreen()
                            else
                                if isLetteredNumberlessDungeon then  // OwGettableLocations does not contain already-marked spots
                                    owLocatorTilesZone.[i,j].MakeGreen()
                                else
                                    owLocatorTilesZone.[i,j].MakeRed()
        )
    showLocatorInstanceFunc <- (fun f ->
        routeDrawingCanvas.Children.Clear()
        for i = 0 to 15 do
            for j = 0 to 7 do
                if f(i,j) && TrackerModel.overworldMapMarks.[i,j].Current() = -1 then
                    owLocatorTilesZone.[i,j].MakeGreen()
        )
    showShopLocatorInstanceFunc <- (fun item ->
        routeDrawingCanvas.Children.Clear()
        for i = 0 to 15 do
            for j = 0 to 7 do
                let cur = TrackerModel.overworldMapMarks.[i,j].Current()
                if MapStateProxy(cur).IsThreeItemShop && 
                        (cur = item || (TrackerModel.getOverworldMapExtraData(i,j,TrackerModel.MapSquareChoiceDomainHelper.SHOP) = TrackerModel.MapSquareChoiceDomainHelper.ToItem(item))) then
                    owLocatorTilesZone.[i,j].MakeGreen()
        )
    showLocator <- (fun sld ->
        match sld with
        | ShowLocatorDescriptor.DungeonNumber(n) ->
            let mutable index = -1
            for i = 0 to 7 do
                if TrackerModel.GetDungeon(i).LabelChar = char(int '1' + n) then
                    index <- i
            let showHint() =
                let hinted_zone = TrackerModel.GetLevelHint(n)
                if hinted_zone <> TrackerModel.HintZone.UNKNOWN then
                    showLocatorHintedZone(hinted_zone,true)
            if index <> -1 then
                let loc = TrackerModel.mapStateSummary.DungeonLocations.[index]
                if loc <> TrackerModel.NOTFOUND then
                    showLocatorExactLocation(loc)
                else
                    showHint()
            else
                showHint()
        | ShowLocatorDescriptor.DungeonIndex(i) ->
            let loc = TrackerModel.mapStateSummary.DungeonLocations.[i]
            if loc <> TrackerModel.NOTFOUND then
                showLocatorExactLocation(loc)
            else
                if TrackerModel.IsHiddenDungeonNumbers() then
                    let label = TrackerModel.GetDungeon(i).LabelChar
                    if label >= '1' && label <= '8' then
                        let index = int label - int '1'
                        let hinted_zone = TrackerModel.GetLevelHint(index)
                        if hinted_zone <> TrackerModel.HintZone.UNKNOWN then
                            showLocatorHintedZone(hinted_zone,true)
                else
                    let hinted_zone = TrackerModel.GetLevelHint(i)
                    if hinted_zone <> TrackerModel.HintZone.UNKNOWN then
                        showLocatorHintedZone(hinted_zone,false)
        | ShowLocatorDescriptor.Sword1 ->
            let loc = TrackerModel.mapStateSummary.Sword1Location
            if loc <> TrackerModel.NOTFOUND then
                showLocatorExactLocation(loc)
        | ShowLocatorDescriptor.Sword2 ->
            let loc = TrackerModel.mapStateSummary.Sword2Location
            if loc <> TrackerModel.NOTFOUND then
                showLocatorExactLocation(loc)
            else
                let hinted_zone = TrackerModel.GetLevelHint(9)
                if hinted_zone <> TrackerModel.HintZone.UNKNOWN then
                    showLocatorHintedZone(hinted_zone,false)
        | ShowLocatorDescriptor.Sword3 ->
            let loc = TrackerModel.mapStateSummary.Sword3Location
            if loc <> TrackerModel.NOTFOUND then
                showLocatorExactLocation(loc)
            else
                let hinted_zone = TrackerModel.GetLevelHint(10)
                if hinted_zone <> TrackerModel.HintZone.UNKNOWN then
                    showLocatorHintedZone(hinted_zone,false)
        )
    hideLocator <- (fun () ->
        if not zone_checkbox.IsChecked.HasValue || not zone_checkbox.IsChecked.Value then changeZoneOpacity(TrackerModel.HintZone.UNKNOWN,false)
        allOwMapZoneBlackCanvases |> Array2D.iteri (fun _x _y zbc -> zbc.Opacity <- 0.0)
        for i = 0 to 15 do
            for j = 0 to 7 do
                owLocatorTilesZone.[i,j].Hide()
        owLocatorCanvas.Children.Clear()
        ensureRespectingOwGettableScreensCheckBox()
        )

    addZoneName(TrackerModel.HintZone.DEATH_MOUNTAIN, "DEATH\nMOUNTAIN", 2.5, 0.3)
    addZoneName(TrackerModel.HintZone.GRAVE,          "GRAVE", 1.5, 2.8)
    addZoneName(TrackerModel.HintZone.DEAD_WOODS,     "DEAD\nWOODS", 1.4, 5.3)
    addZoneName(TrackerModel.HintZone.LAKE,           "LAKE 1", 10.2, 0.1)
    addZoneName(TrackerModel.HintZone.LAKE,           "LAKE 2", 5.5, 3.5)
    addZoneName(TrackerModel.HintZone.LAKE,           "LAKE 3", 9.4, 5.5)
    addZoneName(TrackerModel.HintZone.RIVER,          "RIVER 1", 7.3, 1.1)
    addZoneName(TrackerModel.HintZone.RIVER,          "RIV\nER2", 5.1, 6.2)
    addZoneName(TrackerModel.HintZone.NEAR_START,     "START", 7.3, 6.2)
    addZoneName(TrackerModel.HintZone.DESERT,         "DESERT", 10.3, 3.1)
    addZoneName(TrackerModel.HintZone.FOREST,         "FOREST", 12.3, 5.1)
    addZoneName(TrackerModel.HintZone.LOST_HILLS,     "LOST\nHILLS", 12.4, 0.3)
    addZoneName(TrackerModel.HintZone.COAST,          "COAST", 14.3, 2.7)

    // timeline, options menu, reminders
    let moreOptionsButton = Graphics.makeButton("Options...", Some(12.), Some(Brushes.Orange))
    moreOptionsButton.MaxHeight <- 25.
    moreOptionsButton.Measure(new Size(System.Double.PositiveInfinity, 25.))

    let optionsCanvas = OptionsMenu.makeOptionsCanvas(appMainCanvas.Width, true)
    optionsCanvas.Opacity <- 1.
    optionsCanvas.IsHitTestVisible <- true

    let theTimeline1 = new Timeline.Timeline(21., 4, 60, 5, appMainCanvas.Width-48., 1, "0h", "30m", "1h", moreOptionsButton.DesiredSize.Width-24.)
    let theTimeline2 = new Timeline.Timeline(21., 4, 60, 5, appMainCanvas.Width-48., 2, "0h", "1h", "2h", moreOptionsButton.DesiredSize.Width-24.)
    let theTimeline3 = new Timeline.Timeline(21., 4, 60, 5, appMainCanvas.Width-48., 3, "0h", "1.5h", "3h", moreOptionsButton.DesiredSize.Width-24.)
    theTimeline1.Canvas.Opacity <- 1.
    theTimeline2.Canvas.Opacity <- 0.
    theTimeline3.Canvas.Opacity <- 0.
    let updateTimeline(minute) =
        if minute <= 60 then
            theTimeline1.Canvas.Opacity <- 1.
            theTimeline2.Canvas.Opacity <- 0.
            theTimeline3.Canvas.Opacity <- 0.
            theTimeline1.Update(minute, timelineItems)
        elif minute <= 120 then
            theTimeline1.Canvas.Opacity <- 0.
            theTimeline2.Canvas.Opacity <- 1.
            theTimeline3.Canvas.Opacity <- 0.
            theTimeline2.Update(minute, timelineItems)
        else
            theTimeline1.Canvas.Opacity <- 0.
            theTimeline2.Canvas.Opacity <- 0.
            theTimeline3.Canvas.Opacity <- 1.
            theTimeline3.Update(minute, timelineItems)
    canvasAdd(appMainCanvas, theTimeline1.Canvas, 24., START_TIMELINE_H)
    canvasAdd(appMainCanvas, theTimeline2.Canvas, 24., START_TIMELINE_H)
    canvasAdd(appMainCanvas, theTimeline3.Canvas, 24., START_TIMELINE_H)

    canvasAdd(appMainCanvas, moreOptionsButton, 0., START_TIMELINE_H)
    let mutable popupIsActive = false
    moreOptionsButton.Click.Add(fun _ -> 
        if not popupIsActive then
            popupIsActive <- true
            let wh = new System.Threading.ManualResetEvent(false)
            async {
                do! CustomComboBoxes.DoModalDocked(cm, wh, Dock.Bottom, optionsCanvas)
                TrackerModel.Options.writeSettings()
                popupIsActive <- false
                } |> Async.StartImmediate
        )

    // reminder display
    let cxt = System.Threading.SynchronizationContext.Current 
    let reminderDisplayOuterDockPanel = new DockPanel(Width=OMTW*16., Height=THRU_TIMELINE_H-START_TIMELINE_H, Opacity=0., LastChildFill=false)
    let reminderDisplayInnerDockPanel = new DockPanel(LastChildFill=false, Background=Brushes.Black)
    let reminderDisplayInnerBorder = new Border(Child=reminderDisplayInnerDockPanel, BorderThickness=Thickness(3.), BorderBrush=Brushes.Lime, HorizontalAlignment=HorizontalAlignment.Right)
    DockPanel.SetDock(reminderDisplayInnerBorder, Dock.Top)
    reminderDisplayOuterDockPanel.Children.Add(reminderDisplayInnerBorder) |> ignore
    canvasAdd(appMainCanvas, reminderDisplayOuterDockPanel, 0., START_TIMELINE_H)
    reminderAgent <- MailboxProcessor.Start(fun inbox -> 
        let rec messageLoop() = async {
            let! (text,shouldRemindVoice,icons,shouldRemindVisual) = inbox.Receive()
            do! Async.SwitchToContext(cxt)
            if not(TrackerModel.Options.IsMuted) then
                let sp = new StackPanel(Orientation=Orientation.Horizontal, Background=Brushes.Black, Margin=Thickness(6.))
                for i in icons do
                    i.Margin <- Thickness(3.)
                    sp.Children.Add(i) |> ignore
                let iconCount = sp.Children.Count
                if shouldRemindVisual then
                    Graphics.PlaySoundForReminder()
                reminderDisplayInnerDockPanel.Children.Clear()
                DockPanel.SetDock(sp, Dock.Right)
                reminderDisplayInnerDockPanel.Children.Add(sp) |> ignore
                if shouldRemindVisual then
                    reminderDisplayOuterDockPanel.Opacity <- 1.
                do! Async.SwitchToThreadPool()
                if shouldRemindVisual then
                    do! Async.Sleep(200) // give reminder clink sound time to play
                let startSpeakTime = DateTime.Now
                if shouldRemindVoice then
                    voice.Speak(text) 
                if shouldRemindVisual then
                    let minimumDuration = TimeSpan.FromSeconds(max 3 iconCount |> float)  // ensure at least 3s, and at least 1s per icon
                    let elapsed = DateTime.Now - startSpeakTime
                    if elapsed < minimumDuration then
                        let ms = (minimumDuration - elapsed).TotalMilliseconds |> int
                        do! Async.Sleep(ms)   // ensure ui displayed a minimum time
                do! Async.SwitchToContext(cxt)
                reminderDisplayOuterDockPanel.Opacity <- 0.
            return! messageLoop()
            }
        messageLoop()
        )

    appMainCanvas.MouseDown.Add(fun _ -> 
        let w = Window.GetWindow(appMainCanvas)
        Input.Keyboard.ClearFocus()                // ensure that clicks outside the Notes area de-focus it, by clearing keyboard focus...
        Input.FocusManager.SetFocusedElement(w, null)  // ... and by removing its logical mouse focus
        Input.Keyboard.Focus(w) |> ignore  // refocus keyboard to main window so MyKey still works
        )  

    // broadcast window
    let makeBroadcastWindow(size, showOverworldMagnifier) =
        let W = 768.
        let broadcastWindow = new Window()
        broadcastWindow.Title <- "Z-Tracker broadcast"
        broadcastWindow.ResizeMode <- ResizeMode.NoResize
        broadcastWindow.SizeToContent <- SizeToContent.Manual
        broadcastWindow.WindowStartupLocation <- WindowStartupLocation.Manual
        broadcastWindow.Left <- 0.0
        broadcastWindow.Top <- 0.0
        broadcastWindow.Width <- (if size=1 then 256. elif size=2 then 512. else 768.) + 16.
        broadcastWindow.Owner <- Application.Current.MainWindow
        broadcastWindow.Background <- Brushes.Black

        let makeViewRect(upperLeft:Point, lowerRight:Point) =
            let vb = new VisualBrush(appMainCanvas)
            vb.ViewboxUnits <- BrushMappingMode.Absolute
            vb.Viewbox <- Rect(upperLeft, lowerRight)
            vb.Stretch <- Stretch.None
            let bwRect = new Shapes.Rectangle(Width=vb.Viewbox.Width, Height=vb.Viewbox.Height)
            bwRect.Fill <- vb
            bwRect
        let dealWithPopups(topViewboxRelativeToApp, topViewboxRelativeToThisBroadcast, c) =
            let popups = new System.Collections.Generic.Stack<_>()
            let popupCanvasArea = new Canvas()
            canvasAdd(c, popupCanvasArea, 0., 0.)
            cm.AfterCreatePopupCanvas.Add(fun pc ->
                let vb = new VisualBrush(pc)
                vb.ViewboxUnits <- BrushMappingMode.Absolute
                vb.Viewbox <- Rect(Point(0.,0.), Point(appMainCanvas.Width,appMainCanvas.Height))
                vb.Stretch <- Stretch.None
                let bwRect = new Shapes.Rectangle(Width=vb.Viewbox.Width, Height=vb.Viewbox.Height)
                bwRect.Fill <- vb
                popups.Push(bwRect)
                canvasAdd(popupCanvasArea, bwRect, 0., topViewboxRelativeToThisBroadcast - topViewboxRelativeToApp)
                )
            cm.BeforeDismissPopupCanvas.Add(fun _pc ->
                if popups.Count > 0 then   // we can underflow if the user turns on the broadcast window mid-game, as the options window was a popup when the broadcast began
                    let bwRect = popups.Pop()
                    popupCanvasArea.Children.Remove(bwRect)
                )

        let timeline = makeViewRect(Point(0.,START_TIMELINE_H), Point(W,THRU_TIMELINE_H))
    
        // construct the top broadcast canvas (topc)
        let notesY = START_DUNGEON_AND_NOTES_AREA_H + blockerGrid.Height
        let top = makeViewRect(Point(0.,0.), Point(W,notesY))
        let H = top.Height + (THRU_TIMELINE_H - START_TIMELINE_H)  // the top one is the larger of the two, so always have window that size
        let topc = new Canvas(Height=H)
        canvasAdd(topc, top, 0., 0.)
        let notes = makeViewRect(Point(BLOCKERS_AND_NOTES_OFFSET,notesY), Point(W,notesY+blockerGrid.Height))
        canvasAdd(topc, notes, 0., START_DUNGEON_AND_NOTES_AREA_H)
        let blackArea = new Canvas(Width=BLOCKERS_AND_NOTES_OFFSET*2.-W, Height=blockerGrid.Height, Background=Brushes.Black)
        canvasAdd(topc, blackArea, W-BLOCKERS_AND_NOTES_OFFSET, START_DUNGEON_AND_NOTES_AREA_H)
        if showOverworldMagnifier then
            let magnifierView = new Canvas(Width=DTOCW, Height=DTOCH, Background=new VisualBrush(dungeonTabsOverlayContent))
            Canvas.SetLeft(magnifierView, 0.)
            Canvas.SetBottom(magnifierView, 0.)
            topc.Children.Add(magnifierView) |> ignore
        dealWithPopups(0., 0., topc)

        // construct the bottom broadcast canvas (bottomc)
        let dun = makeViewRect(Point(0.,THRU_MAIN_MAP_AND_ITEM_PROGRESS_H), Point(W,START_TIMELINE_H))
        let tri = makeViewRect(Point(0.,0.), Point(W,150.))
        let pro = makeViewRect(Point(ITEM_PROGRESS_FIRST_ITEM,THRU_MAP_AND_LEGEND_H), Point(ITEM_PROGRESS_FIRST_ITEM + 13.*30.,THRU_MAIN_MAP_AND_ITEM_PROGRESS_H))
        pro.HorizontalAlignment <- HorizontalAlignment.Left
        pro.Margin <- Thickness(20.,0.,0.,0.)
        let owm = makeViewRect(Point(0.,150.), Point(W,THRU_MAIN_MAP_H))
        let sp = new StackPanel(Orientation=Orientation.Vertical)
        sp.Children.Add(tri) |> ignore
        sp.Children.Add(pro) |> ignore
        sp.Children.Add(dun) |> ignore
        let bottomc = new Canvas()
        canvasAdd(bottomc, sp, 0., 0.)
        let afterSoldItemBoxesX = OW_ITEM_GRID_LOCATIONS.OFFSET + 150.
        let blackArea = new Canvas(Width=120., Height=30., Background=Brushes.Black)
        canvasAdd(bottomc, blackArea, afterSoldItemBoxesX, 30.)
        let scale = (W - afterSoldItemBoxesX) / W
        owm.RenderTransform <- new ScaleTransform(scale,scale)
        canvasAdd(bottomc, owm, afterSoldItemBoxesX, 60.)
        let kitty = new Image()
        let imageStream = Graphics.GetResourceStream("CroppedBrianKitty.png")
        kitty.Source <- System.Windows.Media.Imaging.BitmapFrame.Create(imageStream)
        kitty.Width <- 60.
        kitty.Height <- 60.
        canvasAdd(bottomc, kitty, afterSoldItemBoxesX+90. + 20., 0.)
        dealWithPopups(THRU_MAIN_MAP_AND_ITEM_PROGRESS_H, 180., bottomc)

        // draw fake mice on top level 
        let addFakeMouse(c:Canvas) =
            let fakeMouse = new Shapes.Polygon(Fill=Brushes.White)
            fakeMouse.Points <- new PointCollection([Point(0.,0.); Point(12.,6.); Point(6.,12.)])
            c.Children.Add(fakeMouse) |> ignore
            fakeMouse
        let fakeTopMouse = addFakeMouse(topc)
        let fakeBottomMouse = addFakeMouse(bottomc)

        // set up the main broadcast window
        broadcastWindow.Height <- H + 40.
        let dp = new DockPanel(Width=W)
        dp.UseLayoutRounding <- true
        DockPanel.SetDock(timeline, Dock.Bottom)
        dp.Children.Add(timeline) |> ignore
        dp.Children.Add(topc) |> ignore
        
        if size=1 || size=2 then
            let factor = if size=1 then 0.333333 else 0.666666
            let trans = new ScaleTransform(factor, factor)
            dp.LayoutTransform <- trans
            broadcastWindow.Height <- H*factor + 40.
        broadcastWindow.Content <- dp
        
        let mutable isUpper = true
        cm.RootCanvas.MouseMove.Add(fun ea ->   // we need RootCanvas to see mouse moving in popups
            let mousePos = ea.GetPosition(appMainCanvas)
            if mousePos.Y < THRU_MAIN_MAP_AND_ITEM_PROGRESS_H then
                if not isUpper then
                    if cm.PopupCanvasStack.Count=0 then  // don't switch panes if a popup is active
                        isUpper <- true
                        dp.Children.RemoveAt(1)
                        dp.Children.Add(topc) |> ignore
                Canvas.SetLeft(fakeTopMouse, mousePos.X)
                Canvas.SetTop(fakeTopMouse, mousePos.Y)
            else
                if isUpper then
                    if cm.PopupCanvasStack.Count=0 then  // don't switch panes if a popup is active
                        isUpper <- false
                        dp.Children.RemoveAt(1)
                        dp.Children.Add(bottomc) |> ignore
                Canvas.SetLeft(fakeBottomMouse, mousePos.X)
                if mousePos.Y > START_TIMELINE_H then
                    // The timeline is docked to the bottom in both the upper and lower views.
                    // There is 'dead space' below the dungeons area and above the timeline in the broadcast window.
                    // The fakeMouse should 'jump over' this dead space so that mouse-gestures in the timeline show in the right spot on the timeline.
                    // This does mean that certain areas of the options-pane popup won't be fakeMouse-displayed correctly, but timeline is more important.
                    let yDistanceMouseToBottom = appMainCanvas.Height - mousePos.Y
                    Canvas.SetTop(fakeBottomMouse, H - yDistanceMouseToBottom)
                else
                    Canvas.SetTop(fakeBottomMouse, mousePos.Y - THRU_MAIN_MAP_AND_ITEM_PROGRESS_H + 180.)
            )
        broadcastWindow
    let mutable broadcastWindow = null
    if TrackerModel.Options.ShowBroadcastWindow.Value then
        broadcastWindow <- makeBroadcastWindow(TrackerModel.Options.BroadcastWindowSize, TrackerModel.Options.BroadcastWindowIncludesOverworldMagnifier.Value)
        broadcastWindow.Show()
        refocusMainWindow()
    OptionsMenu.broadcastWindowOptionChanged.Publish.Add(fun () ->
        // close existing
        if broadcastWindow<>null then
            broadcastWindow.Close()
            broadcastWindow <- null
        // maybe restart
        if TrackerModel.Options.ShowBroadcastWindow.Value then
            broadcastWindow <- makeBroadcastWindow(TrackerModel.Options.BroadcastWindowSize, TrackerModel.Options.BroadcastWindowIncludesOverworldMagnifier.Value)
            broadcastWindow.Show()
            refocusMainWindow()
        )

    canvasAdd(appMainCanvas, spotSummaryCanvas, 50., 30.)  // height chosen to make broadcast-window-cutoff be reasonable

    TrackerModel.forceUpdate()
    updateTimeline


