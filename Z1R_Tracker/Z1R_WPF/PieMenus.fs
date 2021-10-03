﻿module PieMenus

open System.Windows.Controls
open System.Windows.Media
open System.Windows

let canvasAdd = Graphics.canvasAdd
let OMTW = Graphics.OMTW

let takeAnyW = 330.
let takeAnyH = 220.
let BT = 5.
let rightMarginSize = 40.

let resizeImage(screenshotBMP) =
    let image = Graphics.BMPtoImage screenshotBMP
    image.Width <- takeAnyW
    image.Height <- takeAnyH
    image.Stretch <- Stretch.UniformToFill
    image
let makeSkippedHeart() =
    let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
    canvasAdd(c, Graphics.BMPtoImage(Graphics.owHeartEmpty_bmp), 0., 0.)
    CustomComboBoxes.placeSkippedItemXDecoration(c)
    c
let makeItemBox(itemBMP, yesno) =
    let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
    c.Children.Add(new Shapes.Rectangle(Width=30., Height=30., Stroke=yesno, StrokeThickness=3.0)) |> ignore
    canvasAdd(c, Graphics.BMPtoImage itemBMP, 4., 4.)
    c
let makeXtoY(x,y,rightMargin,group:StackPanel) =
    let row = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center, Margin=Thickness(0.,0.,rightMargin,0.))
    row.Children.Add(x) |> ignore
    row.Children.Add(Graphics.BMPtoImage Graphics.iconRightArrow_bmp) |> ignore
    row.Children.Add(y) |> ignore
    group.Children.Add(row) |> ignore

let TAKE_ANY = TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY

let takeAnyCandlePanel = 
    let c1 = makeItemBox(Graphics.blue_candle_bmp,CustomComboBoxes.no)
    let c2 = makeItemBox(Graphics.blue_candle_bmp,CustomComboBoxes.yes)
    let col = new StackPanel(Orientation=Orientation.Vertical, Background=Brushes.Black)
    let group = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    makeXtoY(Graphics.BMPtoImage Graphics.owHeartEmpty_bmp, makeSkippedHeart(), rightMarginSize, group)
    makeXtoY(Graphics.BMPtoImage Graphics.theInteriorBmpTable.[TAKE_ANY].[0], Graphics.BMPtoImage Graphics.theInteriorBmpTable.[TAKE_ANY].[1], rightMarginSize, group)
    makeXtoY(c1, c2, 0., group)
    col.Children.Add(group) |> ignore
    col.Children.Add(resizeImage Graphics.takeAnyCandleBMP) |> ignore
    let b = new Border(Child=col, BorderBrush=Brushes.Gray, BorderThickness=Thickness(BT), Width=takeAnyW+2.*BT, Height=takeAnyH+2.*BT+30., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    b

let takeAnyPotionPanel = 
    let col = new StackPanel(Orientation=Orientation.Vertical, Background=Brushes.Black)
    let group = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    makeXtoY(Graphics.BMPtoImage Graphics.owHeartEmpty_bmp, makeSkippedHeart(), rightMarginSize, group)
    makeXtoY(Graphics.BMPtoImage Graphics.theInteriorBmpTable.[TAKE_ANY].[0], Graphics.BMPtoImage Graphics.theInteriorBmpTable.[TAKE_ANY].[1], 0., group)
    col.Children.Add(group) |> ignore
    col.Children.Add(resizeImage Graphics.takeAnyPotionBMP) |> ignore
    let b = new Border(Child=col, BorderBrush=Brushes.Gray, BorderThickness=Thickness(BT), Width=takeAnyW+2.*BT, Height=takeAnyH+2.*BT+30., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    b

let takeAnyHeartPanel = 
    let col = new StackPanel(Orientation=Orientation.Vertical, Background=Brushes.Black)
    let group = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    makeXtoY(Graphics.BMPtoImage Graphics.owHeartEmpty_bmp, Graphics.BMPtoImage Graphics.owHeartFull_bmp, rightMarginSize, group)
    makeXtoY(Graphics.BMPtoImage Graphics.theInteriorBmpTable.[TAKE_ANY].[0], Graphics.BMPtoImage Graphics.theInteriorBmpTable.[TAKE_ANY].[1], 0., group)
    col.Children.Add(group) |> ignore
    col.Children.Add(resizeImage Graphics.takeAnyHeartBMP) |> ignore
    let b = new Border(Child=col, BorderBrush=Brushes.Gray, BorderThickness=Thickness(BT), Width=takeAnyW+6., Height=takeAnyH+2.*BT+30., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    b

let takeAnyLeavePanel = 
    let col = new StackPanel(Orientation=Orientation.Vertical, Background=Brushes.Black)
    let group = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    makeXtoY(Graphics.BMPtoImage Graphics.theInteriorBmpTable.[TAKE_ANY].[0], Graphics.BMPtoImage Graphics.theInteriorBmpTable.[TAKE_ANY].[0], 0., group)
    col.Children.Add(resizeImage Graphics.takeAnyLeaveBMP) |> ignore
    col.Children.Add(group) |> ignore
    let b = new Border(Child=col, BorderBrush=Brushes.Gray, BorderThickness=Thickness(BT), Width=takeAnyW+6., Height=takeAnyH+2.*BT+30., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    b

let TakeAnyPieMenu(cm,h,markTakeAnyAsComplete,onClose) =
    let c = new Canvas(IsHitTestVisible=true)
    let ps = ResizeArray()
    let selfCleanupFuncs = ResizeArray()    
    for p, dock in [takeAnyCandlePanel,Dock.Top; takeAnyPotionPanel,Dock.Left; takeAnyHeartPanel,Dock.Right; takeAnyLeavePanel,Dock.Bottom] do
        let dp = new DockPanel(Width=16.*OMTW-20., Height=h, LastChildFill=false)
        DockPanel.SetDock(p, dock)
        dp.Children.Add(p) |> ignore
        selfCleanupFuncs.Add(fun () -> dp.Children.Clear())  // deparent the panels so we can reuse them
        canvasAdd(c, dp, 10., 10.)
        ps.Add(p)
    let onCloseOrDismiss() =
        ps |> Seq.iter (fun p -> p.BorderBrush <- Brushes.Gray)
        for f in selfCleanupFuncs do f()
        onClose()
    let innerH = h - 2.*takeAnyCandlePanel.Height
    let g = new Grid(Width=16.*OMTW-20., Height=h)
    let circle = new Shapes.Ellipse(Width=innerH, Height=innerH, Stroke=Brushes.LightGray, StrokeThickness=3., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    g.Children.Add(circle) |> ignore
    //let slash = new Shapes.Line(X1=0., Y1=innerH, X2=innerH, Y2=0., Stroke=Brushes.LightGray, StrokeThickness=3., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    //g.Children.Add(slash) |> ignore
    let r = innerH/2.
    let root2 = 1.414
    let delta = r - r/root2
    let lineCanvas = new Canvas(Width=innerH, Height=innerH)
    let slash1 = new Shapes.Line(X1=0., Y1=innerH, X2=delta, Y2=innerH-delta, Stroke=Brushes.LightGray, StrokeThickness=3., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    lineCanvas.Children.Add(slash1) |> ignore
    let slash2 = new Shapes.Line(X1=innerH-delta, Y1=delta, X2=innerH, Y2=0., Stroke=Brushes.LightGray, StrokeThickness=3., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    lineCanvas.Children.Add(slash2) |> ignore
    let backslash1 = new Shapes.Line(X1=0., Y1=0., X2=delta, Y2=delta, Stroke=Brushes.LightGray, StrokeThickness=3., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    lineCanvas.Children.Add(backslash1) |> ignore
    let backslash2 = new Shapes.Line(X1=innerH-delta, Y1=innerH-delta, X2=innerH, Y2=innerH, Stroke=Brushes.LightGray, StrokeThickness=3., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    lineCanvas.Children.Add(backslash2) |> ignore
    g.Children.Add(lineCanvas) |> ignore
    //let backslash = new Shapes.Line(X1=0., Y1=0., X2=innerH, Y2=innerH, Stroke=Brushes.LightGray, StrokeThickness=3., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    //g.Children.Add(backslash) |> ignore
    let outerCircle = new Shapes.Ellipse(Width=innerH*1.414, Height=innerH*1.414, Stroke=Brushes.LightGray, StrokeThickness=3., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    outerCircle.StrokeDashArray <- new DoubleCollection([|1.;2.|])
    g.Children.Add(outerCircle) |> ignore
    canvasAdd(c, g, 10., 10.)
    let center = Point(8.*OMTW,h*0.5)
    let mutable currentSelection = -1
    c.MouseMove.Add(fun ea ->
        let pos = ea.GetPosition(c)
        let vector = Point.Subtract(pos, center)
        let distance = vector.Length
        ps |> Seq.iter (fun p -> p.BorderBrush <- Brushes.Gray)
        currentSelection <- -1
        if distance > innerH/2. then
            if vector.X > 0. && vector.X > abs(vector.Y) then
                ps.[2].BorderBrush <- Brushes.Yellow
                currentSelection <- 2
            elif vector.X < 0. && abs(vector.X) > abs(vector.Y) then
                ps.[1].BorderBrush <- Brushes.Yellow
                currentSelection <- 1
            elif vector.Y < 0. && abs(vector.Y) > abs(vector.X) then
                ps.[0].BorderBrush <- Brushes.Yellow
                currentSelection <- 0
            elif vector.Y > 0. && vector.Y > abs(vector.X) then
                ps.[3].BorderBrush <- Brushes.Yellow
                currentSelection <- 3
        )
    let mutable dismiss = fun()->()
    let click(ea:Input.MouseEventArgs) =
        ea.Handled <- true
        let whichHeart() =
            if TrackerModel.playerProgressAndTakeAnyHearts.GetTakeAnyHeart(0)=0 then 0
            elif TrackerModel.playerProgressAndTakeAnyHearts.GetTakeAnyHeart(1)=0 then 1
            elif TrackerModel.playerProgressAndTakeAnyHearts.GetTakeAnyHeart(2)=0 then 2
            elif TrackerModel.playerProgressAndTakeAnyHearts.GetTakeAnyHeart(3)=0 then 3
            else 
                System.Media.SystemSounds.Asterisk.Play()  // warn the user something is awry
                -1
        if currentSelection=0 then // candle
            let which = whichHeart()
            if which <> -1 then
                TrackerModel.playerProgressAndTakeAnyHearts.SetTakeAnyHeart(which, 2)
            TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueCandle.Set(true)
            markTakeAnyAsComplete(true)
        elif currentSelection=1 then // potion
            let which = whichHeart()
            if which <> -1 then
                TrackerModel.playerProgressAndTakeAnyHearts.SetTakeAnyHeart(which, 2)
            markTakeAnyAsComplete(true)
        elif currentSelection=2 then // heart
            let which = whichHeart()
            if which <> -1 then
                TrackerModel.playerProgressAndTakeAnyHearts.SetTakeAnyHeart(which, 1)
            markTakeAnyAsComplete(true)
        else // leave or cancel
            markTakeAnyAsComplete(false)
    c.MouseDown.Add(fun ea ->
        click(ea)
        dismiss()
        onCloseOrDismiss()
        )
    let mutable isFirstTimeMouseUp = true
    c.MouseUp.Add(fun ea ->
        if isFirstTimeMouseUp && currentSelection = -1 then
            isFirstTimeMouseUp <- false
        else
            click(ea)
            dismiss()
            onCloseOrDismiss()
        )
    Graphics.WarpMouseCursorTo(center)
    dismiss <- CustomComboBoxes.DoModal(cm, 0., 0., c, onCloseOrDismiss)

let TakeAnyPieMenuAsync(cm,h) =
    let mutable r = false
    let wh = new System.Threading.ManualResetEvent(false)
    async {
        let cxt = System.Threading.SynchronizationContext.Current
        TakeAnyPieMenu(cm, h, (fun b -> r <- b), (fun() -> wh.Set() |> ignore))
        let! _ = Async.AwaitWaitHandle(wh)
        wh.Close()
        do! Async.SwitchToContext(cxt)
        return r
    }
