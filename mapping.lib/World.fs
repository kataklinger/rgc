module World

open Print
open Locate

type Frame =
  { features: Feature.Handle list
    original: Matrix.Matrix<byte>
    blured: Matrix.Matrix<byte> }

type World =
  { sketch: Sketch.Sketch
    print: Print.Print
    previous: (Frame * Primitive.Point) option }

let empty =
  { sketch = Sketch.empty
    print = Print.empty
    previous = None }

let update (world: World) (current: Frame) =
  let matches =
    current.features |> Sketch.find world.sketch

  let previousPos =
    match world.previous with
    | Some (_, pos) -> pos
    | None -> 0, 0

  let position =
    { matches = matches
      range = world.sketch.range
      wnd = current.original.Rect
      previous = previousPos }
    |> Locate.byFeatureCount

  let (|Found|NotFound|) (pos: (int * int) option) =
    match pos, world.previous with
    | Some pos, _ -> Found pos
    | _, None -> Found(0, 0)
    | _ -> NotFound

  match position with
  | Found pos ->
      let segment =
        { position = pos
          image = current.original
          motion =
            match world.previous with
            | Some (frame, previousPos) ->
                let delta = Primitive.sub pos previousPos
                Motion.mark 9 delta frame.blured current.blured
            | _ -> Matrix.Matrix<int>(current.original.Width, current.original.Height) }

      { sketch = Sketch.update world.sketch pos current.features
        print = Print.update world.print segment
        previous = Some(current, pos) }

  | NotFound ->
      { world with
          sketch = Sketch.update world.sketch (0, 0) List.empty }

let frame (extractor: Matrix.Matrix<byte> -> Matrix.Matrix<byte> -> Feature.Handle list)
          (img: Matrix.Matrix<byte>)
          =
  let blured = img |> Filter.median 5

  { features = extractor img blured
    original = img
    blured = blured }
