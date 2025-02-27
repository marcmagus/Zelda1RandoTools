﻿module Dungeon

open Avalonia.Media
open Avalonia.Controls
open Avalonia
open Avalonia.Layout

// door colors
let unknown = new SolidColorBrush(Color.FromRgb(30uy, 30uy, 45uy)) :> Brush
let no = new SolidColorBrush(Color.FromRgb(125uy, 0uy, 0uy)) :> Brush
let yes = new SolidColorBrush(Color.FromRgb(60uy,120uy,60uy)) :> Brush
let blackedOut = new SolidColorBrush(Color.FromRgb(15uy, 15uy, 25uy))  :> Brush

[<RequireQualifiedAccess>]
type DoorState = | UNKNOWN | NO | YES | BLACKEDOUT

type Door(state:DoorState, redraw) =
    let mutable state = state
    member _this.State with get() = state and set(x) = state <- x; redraw(x)

type GrabHelper() =
    let mutable isGrabMode = false
    let mutable grabMouseX, grabMouseY = -1, -1
    let grabContiguousRooms = Array2D.zeroCreate 8 8
    let mutable roomStatesIfGrabWereACut = null
    let mutable roomIsCircledIfGrabWereACut = null
    let mutable horizontalDoorsIfGrabWereACut = null
    let mutable verticalDoorsIfGrabWereACut = null

    member this.PreviewGrab(mouseX, mouseY, roomStates:DungeonRoomState.DungeonRoomState[,]) =
        if not this.IsGrabMode || this.HasGrab then
            failwith "bad"
        if roomStates.[mouseX,mouseY].RoomType.IsNotMarked then
            failwith "bad"
        // compute set of contiguous rooms
        let contiguous = Array2D.zeroCreate 8 8
        let q = new System.Collections.Generic.Queue<_>()
        q.Enqueue(mouseX, mouseY)
        while q.Count > 0 do
            let roomX,roomY = q.Dequeue()
            contiguous.[roomX,roomY] <- true
            let unvisited(x,y) = x>=0 && x<=7 && y>=0 && y<=7 && not contiguous.[x,y]
            let attempt(x,y) = if unvisited(x,y) && not(roomStates.[x,y].IsEmpty) then q.Enqueue(x,y)
            attempt(roomX-1,roomY)
            attempt(roomX+1,roomY)
            attempt(roomX,roomY-1)
            attempt(roomX,roomY+1)
        // return them
        contiguous
    member this.StartGrab(mouseX, mouseY, roomStates:DungeonRoomState.DungeonRoomState[,], roomIsCircled:bool[,], horizontalDoors:Door[,], verticalDoors:Door[,]) =
        let contigous = this.PreviewGrab(mouseX, mouseY, roomStates)
        // save them
        grabMouseX <- mouseX
        grabMouseY <- mouseY
        contigous |> Array2D.iteri (fun x y v -> grabContiguousRooms.[x,y] <- v)
        roomStatesIfGrabWereACut <- Array2D.init 8 8 (fun x y -> if contigous.[x,y] then new DungeonRoomState.DungeonRoomState() else roomStates.[x,y].Clone())
        roomIsCircledIfGrabWereACut <- Array2D.init 8 8 (fun x y -> if contigous.[x,y] then false else roomIsCircled.[x,y])
        horizontalDoorsIfGrabWereACut <- Array2D.init 7 8 (fun x y -> if contigous.[x,y] || contigous.[x+1,y] then DoorState.UNKNOWN else horizontalDoors.[x,y].State)
        verticalDoorsIfGrabWereACut <- Array2D.init 8 7 (fun x y -> if contigous.[x,y] || contigous.[x,y+1] then DoorState.UNKNOWN else verticalDoors.[x,y].State)
        contigous

    member this.PreviewDrop(mouseX, mouseY) =
        if not this.IsGrabMode || not this.HasGrab then
            failwith "bad"
        let dx,dy = mouseX-grabMouseX, mouseY-grabMouseY
        let contiguousOk = Array2D.zeroCreate 8 8
        let contiguousWarn = Array2D.zeroCreate 8 8
        for x = 0 to 7 do
            for y = 0 to 7 do
                let i,j = x-dx, y-dy
                if i>=0 && i<=7 && j>=0 && j<=7 then
                    if grabContiguousRooms.[i,j] then
                        if not(roomStatesIfGrabWereACut.[x,y].IsEmpty) then
                            contiguousWarn.[x,y] <- true
                        else
                            contiguousOk.[x,y] <- true
        contiguousOk, contiguousWarn

    member this.DoDrop(mouseX, mouseY, roomStates:DungeonRoomState.DungeonRoomState[,], roomIsCircled:bool[,], horizontalDoors:Door[,], verticalDoors:Door[,]) =  // mutates arrays
        if not this.IsGrabMode || not this.HasGrab then
            failwith "bad"
        let dx,dy = mouseX-grabMouseX, mouseY-grabMouseY
        let oldRoomStates = roomStates |> Array2D.map (fun s -> s.Clone())
        let oldRoomIsCircled = roomIsCircled.Clone() :?> bool[,]
        let oldHorizontalDoors = horizontalDoors |> Array2D.map (fun c -> c.State)
        let oldVerticalDoors = verticalDoors |> Array2D.map (fun c -> c.State)
        roomStatesIfGrabWereACut |> Array2D.iteri (fun x y v -> roomStates.[x,y] <- v)
        roomIsCircledIfGrabWereACut |> Array2D.iteri (fun x y v -> roomIsCircled.[x,y] <- v)
        horizontalDoorsIfGrabWereACut |> Array2D.iteri (fun x y v -> horizontalDoors.[x,y].State <- v)
        verticalDoorsIfGrabWereACut |> Array2D.iteri (fun x y v -> verticalDoors.[x,y].State <- v)
        for x = 0 to 7 do
            for y = 0 to 7 do
                let i,j = x-dx, y-dy
                if i>=0 && i<=7 && j>=0 && j<=7 then
                    if grabContiguousRooms.[i,j] then
                        roomStates.[x,y] <- oldRoomStates.[i,j]
                        roomIsCircled.[x,y] <- oldRoomIsCircled.[i,j]
                        let do_door(target:Door[,], x, y, source:DoorState[,], i, j) =
                            if source.[i,j] = DoorState.YES || source.[i,j] = DoorState.NO then
                                target.[x,y].State <- source.[i,j]
                        if x<7 && i<7 then
                            do_door(horizontalDoors,x,y,oldHorizontalDoors,i,j)  // door right of room
                        if x>0 && i>0 then
                            do_door(horizontalDoors,x-1,y,oldHorizontalDoors,i-1,j)  // door left of room
                        if y<7 && j<7 then
                            do_door(verticalDoors,x,y,oldVerticalDoors,i,j)  // door below room
                        if y>0 && j>0 then
                            do_door(verticalDoors,x,y-1,oldVerticalDoors,i,j-1)  // door above room
        this.Abort()


    member this.ToggleGrabMode() = isGrabMode <- not isGrabMode
    member this.IsGrabMode = isGrabMode
    member this.HasGrab = grabMouseX <> -1
    member this.Abort() =
        isGrabMode <- false
        grabMouseX <- -1
        grabMouseY <- -1
        for x = 0 to 7 do
            for y = 0 to 7 do
                grabContiguousRooms.[x,y] <- false
        roomStatesIfGrabWereACut <- null
        roomIsCircledIfGrabWereACut <- null
    member this.Log() =
        printfn "log"

///////////////////////////////////////////

let flatColorArray = [|
    // dungeon colors
    0x808080
    0xADADAD
    0xE0E0E0

    0x0050F1
    0x4BA0FF
    0xB6D8FF
    
    0x3B34FF
    0x8A84FF
    0xD0CDFF
    
    0x8022E8
    0xD172FF
    0xEDC6FF
    
    0xBB1EA5
    0xFF6DF7
    0xFFC4FC
    
    0xDB294E
    0xFF799E
    0xFFC8D8
    
    0xD74000
    0xFF9047
    0xFFD2B4
    
    0xB15E00
    0xFFAE0A
    0xFFDE9C
    
    0x737900
    0xC4CA00
    0xE7E994
    
    0x2D8B00
    0x7DDC13
    0xCAF19F

    0x008F08
    0x41E157
    0xB2F3BB

    0x008460
    0x21D5B0
    0xA5EEDF

    0x006DB5
    0x25BEFF
    0xA6E5FF

    0x000000   // add black as default
    0x000000   
    0x000000   
    |]
let threeTallColorArray = 
    let a = ResizeArray()
    let mutable i = 0
    while i < flatColorArray.Length do
        a.Add(flatColorArray.[i])
        i <- i + 3
    i <- 1
    while i < flatColorArray.Length do
        a.Add(flatColorArray.[i])
        i <- i + 3
    i <- 2
    while i < flatColorArray.Length do
        a.Add(flatColorArray.[i])
        i <- i + 3
    a.ToArray()

let makeColor = Graphics.makeColor
let canvasAdd = Graphics.canvasAdd

let TRIFORCE_SIZE = 80.
let DrawTriforceMapCore(haves:bool[], labels:string) =
    let N = TRIFORCE_SIZE
    
    let p1 = Point(2.*N, 0.)
    
    let p2 = Point(N, N)
    let p3 = Point(2.*N, N)
    let p4 = Point(3.*N, N)

    let p5 = Point(0., 2.*N)
    let p6 = Point(N, 2.*N)
    let p7 = Point(2.*N, 2.*N)
    let p8 = Point(3.*N, 2.*N)
    let p9 = Point(4.*N, 2.*N)

    let tri(ps:_[]) = new Shapes.Polygon(Stroke=Brushes.DarkSlateBlue, StrokeThickness=2., Fill=Brushes.Black, Points=ps)
    let triforces = [|   // first quest mapping
        tri [| p1; p2; p3 |]
        tri [| p1; p3; p4 |]
        tri [| p2; p5; p6 |]
        tri [| p4; p8; p9 |]
        tri [| p2; p3; p7 |]
        tri [| p2; p6; p7 |]
        tri [| p3; p4; p7 |]
        tri [| p4; p7; p8 |]
        |]
    let labelLocations = [|
        (1.5*N, 0.5*N)
        (2.0*N, 0.5*N)
        (0.5*N, 1.5*N)
        (3.0*N, 1.5*N)
        (1.5*N, 1.0*N)
        (1.0*N, 1.5*N)
        (2.0*N, 1.0*N)
        (2.5*N, 1.5*N)
        |]
    let c = new Canvas(Width=4.*N, Height=2.*N, Background=Brushes.Black)
    for i = 0 to 7 do
        canvasAdd(c, triforces.[i], 0., 0.)
        let x,y = labelLocations.[i]
        let tb = new TextBox(Width=N/2., Height=N/2., IsHitTestVisible=false, BorderThickness=Thickness(0.), FontSize=N/4., FontWeight=FontWeight.Bold, 
                                    VerticalContentAlignment=VerticalAlignment.Center, HorizontalContentAlignment=HorizontalAlignment.Center, 
                                    Text=(sprintf"%c"(labels.ToCharArray().[i])), Foreground=Brushes.White, Background=Brushes.Transparent)
        canvasAdd(c, tb, x, y)
        if haves.[i] then
            triforces.[i].Fill <- Brushes.Orange
            tb.Foreground <- Brushes.Black
    c.Margin <- Thickness(N/8.)
    c
let DrawTriforces1Q(haves) = 
    DrawTriforceMapCore(haves, "12345678")
let DrawTriforces2Q(haves:_[]) = 
    let h = Array.zeroCreate 8
    h.[0] <- haves.[0]
    h.[1] <- haves.[2]
    h.[2] <- haves.[1]
    h.[3] <- haves.[4]
    h.[4] <- haves.[3]
    h.[5] <- haves.[5]
    h.[6] <- haves.[7]
    h.[7] <- haves.[6]
    DrawTriforceMapCore(h, "13254687")

let HiddenDungeonColorChooserPopup(cm:CustomComboBoxes.CanvasManager, tileX, tileY, tileW, tileH, originalColor, dungeonIndex) = async {
    let colors = threeTallColorArray
    let tileCanvas = new Canvas(Width=tileW, Height=tileH)
    let gridElementsSelectablesAndIDs = colors |> Array.mapi (fun i c -> new Canvas(Width=30., Height=30., Background=new SolidColorBrush(makeColor(c))) :> Control, true, i)
    let originalStateIndex = match colors |> Array.tryFindIndex (fun c -> c=originalColor) with Some i -> i | None -> colors.Length-1
    tileCanvas.Background <- new SolidColorBrush(makeColor(colors.[originalStateIndex]))
    let activationDelta = 0
    let gnc = colors.Length / 3
    let gnr = 3
    let gcw,grh = 30,30
    let gx,gy = -60., tileH+20.
    let redrawTile(i) = tileCanvas.Background <- new SolidColorBrush(makeColor(colors.[i]))
    let onClick(_ea, state) =
        TrackerModel.GetDungeon(dungeonIndex).Color <- colors.[state]
        CustomComboBoxes.DismissPopupWithNoResult
    let extraDecorations = []
    let brushes=CustomComboBoxes.ModalGridSelectBrushes.Defaults()
    let gridClickDismissalDoesMouseWarpBackToTileCenter = false
    do! Async.Ignore <| CustomComboBoxes.DoModalGridSelect(cm, tileX, tileY, tileCanvas, gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (gnc, gnr, gcw, grh),
                                gx, gy, redrawTile, onClick, extraDecorations, brushes, gridClickDismissalDoesMouseWarpBackToTileCenter)
    }

let HiddenDungeonNumberChooserPopup(cm:CustomComboBoxes.CanvasManager, tileX, tileY, tileW, tileH, originalLabelChar:char, dungeonIndex) = async {
    // TODO can you choose same # twice?
    let tileCanvas = new Canvas(Width=tileW, Height=tileH, Background=Brushes.Black)
    let dp = new DockPanel(Width=tileW, Height=tileH)
    canvasAdd(tileCanvas, dp, 0., 0.)
    let mkTxt(ch) =
        new TextBox(Width=60., Height=60., FontSize=36., Foreground=Brushes.White, Background=Brushes.Black, IsHitTestVisible=false, 
                    BorderThickness=Thickness(0.), Text=sprintf "%c" ch, VerticalContentAlignment=VerticalAlignment.Center, HorizontalContentAlignment=HorizontalAlignment.Center,
                    VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center)
    let theTB = mkTxt(originalLabelChar)
    dp.Children.Add(theTB) |> ignore
    let gridElementsSelectablesAndIDs = [|
        for ch in "?12345678" do
            yield (mkTxt(ch) :> Control), true, ch
        |]
    let originalStateIndex = 
        let i = "?12345678".IndexOf(originalLabelChar)
        if i = -1 then 0 else i
    let activationDelta = 0
    let gnc,gnr = 3,3
    let gcw,grh = 60,60
    let gx,gy = -73., tileH+20.
    let redrawTile(ch) = theTB.Text <- sprintf "%c" ch
    let onClick(_ea, ch) =
        TrackerModel.GetDungeon(dungeonIndex).LabelChar <- ch
        CustomComboBoxes.DismissPopupWithNoResult
    let extraDecorations = 
        let warnTB = new TextBox(IsHitTestVisible=false, BorderThickness=Thickness(0.), FontSize=16., Margin=Thickness(5.),
                                    VerticalContentAlignment=VerticalAlignment.Center, HorizontalContentAlignment=HorizontalAlignment.Center, 
                                    Text="Don't mark anything other than '?'\nunless you are certain!", Foreground=Brushes.OrangeRed, Background=Brushes.Black)
        let warnBorder = new Border(BorderThickness=Thickness(4.), BorderBrush=Brushes.Gray, Background=Brushes.Black, Child=warnTB)

        let sp = new StackPanel(Orientation=Orientation.Vertical, Margin=Thickness(5.))
        let haves = TrackerModel.GetTriforceHaves()
        let makeTB(text) = new TextBox(Width=TRIFORCE_SIZE*3.8, IsHitTestVisible=false, BorderThickness=Thickness(0.), FontSize=16., 
                                        VerticalContentAlignment=VerticalAlignment.Center, HorizontalContentAlignment=HorizontalAlignment.Center, 
                                        Text=text, Foreground=Brushes.Orange, Background=Brushes.Black)
        sp.Children.Add(makeTB("Reference diagram - don't click here")) |> ignore
        sp.Children.Add(makeTB("(Note: in Mixed Quest dungeons,\n7 and 8 can be swapped)")) |> ignore
        sp.Children.Add(new DockPanel(Height=TRIFORCE_SIZE/3.)) |> ignore
        sp.Children.Add(makeTB("First Quest or Shapes dungeons")) |> ignore
        sp.Children.Add(DrawTriforces1Q(haves)) |> ignore
        sp.Children.Add(new DockPanel(Height=TRIFORCE_SIZE/3.)) |> ignore
        sp.Children.Add(makeTB("Second Quest dungeons")) |> ignore
        sp.Children.Add(DrawTriforces2Q(haves)) |> ignore
        let b = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(4.), Background=Brushes.Black, Child=sp)
        b.PointerPressed.Add(fun ea -> ea.Handled <- true)  // clicking the reference diagram should not close the dialog, people will do it by accident
        [(upcast warnBorder : Control), -137., 284.; (upcast b : Control), 132., -120.]
    let brushes=CustomComboBoxes.ModalGridSelectBrushes.Defaults()
    let gridClickDismissalDoesMouseWarpBackToTileCenter = false
    do! Async.Ignore <| CustomComboBoxes.DoModalGridSelect(cm, tileX, tileY, tileCanvas, gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (gnc, gnr, gcw, grh),
                            gx, gy, redrawTile, onClick, extraDecorations, brushes, gridClickDismissalDoesMouseWarpBackToTileCenter)
    }

let HiddenDungeonCustomizerPopup(cm:CustomComboBoxes.CanvasManager, dungeonIndex, curColor, curLabel, accelerateIntoNumberChooser, warpReturn:Point) = async {
    let wh = new System.Threading.ManualResetEvent(false)
    // setup main visual tree
    let mainDock = new DockPanel(Background=Brushes.Black)
    
    let text = sprintf "Dungeon %c" "ABCDEFGH".[dungeonIndex]
    let tb = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=24., Text=text, IsHitTestVisible=false, 
                            HorizontalContentAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), Margin=Thickness(0.,0.,0.,20.))
    mainDock.Children.Add(tb) |> ignore
    DockPanel.SetDock(tb, Dock.Top)

    let innerDock = new DockPanel(LastChildFill=false)
    let button1Content = new Canvas(Width=60., Height=60., Background=new SolidColorBrush(makeColor(curColor)))
    let button1 = new Button(Content=button1Content, Margin=Thickness(20.,0.,20.,0.), BorderThickness=Thickness(5.))
    let button2Content = new TextBox(Width=60., Height=60., IsHitTestVisible=false, FontSize=36., Foreground=Brushes.White, Background=Brushes.Black, 
                                        Text=sprintf "%c" curLabel, HorizontalContentAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.))
    let button2 = new Button(Content=button2Content, Margin=Thickness(20.,0.,20.,0.), BorderThickness=Thickness(5.), HorizontalAlignment=HorizontalAlignment.Center)
    let b1a = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=12., Text="Change", IsHitTestVisible=false, 
                            HorizontalContentAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), HorizontalAlignment=HorizontalAlignment.Center)
    let b1b = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=12., Text="Color", IsHitTestVisible=false, 
                            HorizontalContentAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), HorizontalAlignment=HorizontalAlignment.Center)
    let b1sp = new StackPanel(Orientation=Orientation.Vertical)
    b1sp.Children.Add(b1a) |> ignore
    b1sp.Children.Add(button1) |> ignore
    b1sp.Children.Add(b1b) |> ignore
    let b2a = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=12., Text="Change", IsHitTestVisible=false, 
                            HorizontalContentAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), HorizontalAlignment=HorizontalAlignment.Center)
    let b2b = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=12., Text="Number", IsHitTestVisible=false, 
                            HorizontalContentAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), HorizontalAlignment=HorizontalAlignment.Center)
    let b2sp = new StackPanel(Orientation=Orientation.Vertical)
    b2sp.Children.Add(b2a) |> ignore
    b2sp.Children.Add(button2) |> ignore
    b2sp.Children.Add(b2b) |> ignore

    innerDock.Children.Add(b1sp) |> ignore
    DockPanel.SetDock(button1, Dock.Left)
    innerDock.Children.Add(b2sp) |> ignore
    DockPanel.SetDock(button2, Dock.Right)

    mainDock.Children.Add(innerDock) |> ignore

    let theBorder = new Border(BorderBrush=Brushes.Black, BorderThickness=Thickness(10.), Child=mainDock)
    let theBorder = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(4.), Child=theBorder)

    // hook up the button actions
    let mutable popupIsActive = false
    button1.Click.Add(fun _ ->
        if not popupIsActive then
            popupIsActive <- true
            let pos = button1Content.TranslatePoint(Point(), cm.AppMainCanvas).Value
            async {
                do! HiddenDungeonColorChooserPopup(cm, pos.X, pos.Y, button1Content.Width, button1Content.Height, curColor, dungeonIndex)
                popupIsActive <- false
                wh.Set() |> ignore
                } |> Async.StartImmediate
        )
    let button2Body() =
        if not popupIsActive then
            popupIsActive <- true
            let pos = button2Content.TranslatePoint(Point(), cm.AppMainCanvas).Value
            async {
                do! HiddenDungeonNumberChooserPopup(cm, pos.X, pos.Y, button2Content.Width, button2Content.Height, curLabel, dungeonIndex)
                popupIsActive <- false
                wh.Set() |> ignore
                } |> Async.StartImmediate
    button2.Click.Add(fun _ -> button2Body())

    // add main element and extra decorations 
    let popupCanvas = new Canvas()
    canvasAdd(popupCanvas, theBorder, 0., 0.)
    
    let mainX,mainY = 14.,165.

    let dungeonColorCanvas = new Canvas(Width=30., Height=30.)
    canvasAdd(popupCanvas, dungeonColorCanvas, float dungeonIndex*30. - mainX, 0. - mainY)
    CustomComboBoxes.MakePrettyDashes(dungeonColorCanvas, Brushes.Lime, 30., 30., 3., 2., 1.)

    //let guideline = new Shapes.Line(StartPoint=Point(float dungeonIndex*30. - mainX + 15., 36. - mainY), EndPoint=Point(0.,0.), Stroke=Brushes.Gray, StrokeThickness=3.)
    //canvasAdd(popupCanvas, guideline, 0., 0.)

    if accelerateIntoNumberChooser then
        let mutable firstTime = true
        button2.LayoutUpdated.Add(fun _ -> 
            if firstTime then
                firstTime <- false
                button2Body())
    do! CustomComboBoxes.DoModal(cm, wh, mainX, mainY, popupCanvas)
    Graphics.WarpMouseCursorTo(warpReturn)
    }

// Zelda-like minimap

let BLUE = System.Drawing.Color.FromArgb(71, 47, 228)
let MakeMiniMiniMapBmp() =
    let bmp = new System.Drawing.Bitmap(8, 8)
    for x = 0 to 7 do
        for y = 0 to 7 do
            bmp.SetPixel(x,y,System.Drawing.Color.Black)
    for x = 0 to 6 do
        for y = 0 to 2 do
            bmp.SetPixel(x,y,BLUE)
        for y = 4 to 6 do
            bmp.SetPixel(x,y,BLUE)
    bmp
let MakeLoZMinimapDisplayBmp(rooms:bool[,], letter) =
    let YELLOW = System.Drawing.Color.FromArgb(215, 152, 42)
    let bmp = new System.Drawing.Bitmap(80, 40)
    for x = 0 to 79 do
        for y = 0 to 39 do
            bmp.SetPixel(x,y,System.Drawing.Color.Black)
    let mutable xoff = 0
    let w(x,y) = bmp.SetPixel(xoff+x,y,System.Drawing.Color.White)
    // L
    for x = 1 to 3 do
        for y = 0 to 6 do
            w(x,y)
    for x = 4 to 6 do
        w(x,6)
    xoff <- 8
    // E
    for x = 0 to 6 do
        w(x,0)
        w(x,6)
    for x = 0 to 5 do
        w(x,3)
    for x = 0 to 1 do
        for y = 0 to 6 do
            w(x,y)
    xoff <- 16
    // V
    for y = 0 to 3 do
        w(0,y)
        w(1,y)
        w(5,y)
        w(6,y)
    for y = 3 to 4 do
        w(1,y)
        w(2,y)
        w(4,y)
        w(5,y)
    w(3,4)
    for x = 2 to 4 do
        w(x,5)
    w(3,6)
    xoff <- 24
    // E
    for x = 0 to 6 do
        w(x,0)
        w(x,6)
    for x = 0 to 5 do
        w(x,3)
    for x = 0 to 1 do
        for y = 0 to 6 do
            w(x,y)
    xoff <- 32
    // L
    for x = 1 to 3 do
        for y = 0 to 6 do
            w(x,y)
    for x = 4 to 6 do
        w(x,6)
    xoff <- 40
    // -
    for x = 0 to 5 do
        w(x,3)
    xoff <- 48
    // todo
    ignore(letter)
    // _
    for x = 0 to 6 do
        w(x,6)
    for i = 0 to 7 do
        for j = 0 to 7 do
            if rooms.[i,j] then
                let x = i*8
                let y = j*4 + 8
                for dx = 0 to 6 do
                    for dy = 0 to 2 do
                        bmp.SetPixel(x+dx, y+dy, BLUE)
    // rupee
    xoff <- 72
    let mutable yoff = 8
    let pattern = """
...XXXXX
..XXXX.X
.XXXXX.X
XXXXXX.X
XXXXX...
XXXX.XX.
X...XX..
XXX.X..."""          .Split([|'\r'; '\n'|], System.StringSplitOptions.RemoveEmptyEntries)
    let b(x,y) = bmp.SetPixel(xoff+x,yoff+y,YELLOW)
    for x = 0 to 7 do
        for y = 0 to 7 do
            if pattern.[y].Chars(x)='X' then b(x,y)
    // key
    yoff <- 24
    let pattern = """
....XXX.
...X..XX
...X...X
....X..X
...X.XX.
..X.....
.X.X....
X.X....."""          .Split([|'\r'; '\n'|], System.StringSplitOptions.RemoveEmptyEntries)
    for x = 0 to 7 do
        for y = 0 to 7 do
            if pattern.[y].Chars(x)='X' then b(x,y)
    // bomb
    yoff <- 32
    let pattern = """
.......o
..XXXX.o
.XoXXXX.
XoXXXXXX
XoXXXXXX
XXXXXXXX
.XXXXXX.
..XXXX.."""          .Split([|'\r'; '\n'|], System.StringSplitOptions.RemoveEmptyEntries)
    let w(x,y) = bmp.SetPixel(xoff+x,yoff+y,System.Drawing.Color.White)
    let b(x,y) = bmp.SetPixel(xoff+x,yoff+y,BLUE)
    for x = 0 to 7 do
        for y = 0 to 7 do
            if pattern.[y].Chars(x)='X' then b(x,y)
            elif pattern.[y].Chars(x)='o' then w(x,y)
    bmp
