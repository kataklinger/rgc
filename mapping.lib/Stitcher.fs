module Stitcher

open Window

type IStitchingObserver =
  abstract WindowUpdate: int -> Matrix.Matrix<byte> -> Primitive.Rect -> bool -> unit
  abstract WorldFrame: int -> Matrix.Matrix<byte> -> unit
  abstract WorldUpdate: System.Func<Matrix.Matrix<byte>> -> unit
  abstract WorldComplete: Matrix.Matrix<byte> -> unit

let loop (observer: IStitchingObserver) (frames: Matrix.Matrix<byte> seq) =
  let oWindow (no: int) (window: WindowState) =
    match window with
    | Incomplete u -> observer.WindowUpdate no (u.processed |> List.head) u.region false
    | Complete u -> observer.WindowUpdate no (u.delayed |> List.rev |> List.head) u.region true

    window

  let oFrame (no: int) (frame: Matrix.Matrix<byte>) =
    frame |> observer.WorldFrame no
    frame

  let oWorld (world: World.World) =
    observer.WorldUpdate(fun () -> Print.quickPlot world.print)
    world

  let window =
    frames
    |> Seq.mapi (fun no img -> no, img)
    |> Seq.scan (fun state (no, img) ->
         match state with
         | Incomplete p -> img |> Window.update p |> oWindow no
         | _ -> state) (Incomplete Window.Pending.empty)
    |> Seq.tryFind (function
         | Complete _ -> true
         | _ -> false)
    |> Option.defaultValue (Incomplete Window.Pending.empty)

  match window with
  | Complete w ->
      let (top, left), (bottom, right) = w.region

      let reframe frame =
        frame
        |> Matrix.frameZero -top -left (bottom - frame.Height) (right - frame.Width)

      let world =
        frames
        |> Seq.mapi (fun no img -> img |> reframe |> oFrame no)
        |> Seq.map (World.frame Corner.extract)
        |> Seq.fold (fun world frame -> frame |> World.update world |> oWorld) World.empty

      let plot = world.print |> Print.plot
      plot |> observer.WorldComplete
      Some plot
  | _ -> None
