module Window

type Pending =
  { processed: Matrix.Matrix<byte> list
    changes: Matrix.Matrix<byte>
    area: int
    region: Primitive.Rect
    stable: int }

module Pending =
  let empty =
    { processed = []
      changes = Matrix.Matrix<byte>(0, 0)
      area = 0
      region = (0, 0), (0, 0)
      stable = 0 }

type Window =
  { delayed: Matrix.Matrix<byte> list
    region: Primitive.Rect }

let empty =
  { delayed = List.empty
    region = (0, 0), (0, 0) }

type WindowState =
  | Incomplete of Pending
  | Complete of Window

let update (state: Pending) (img: Matrix.Matrix<byte>) =
  let next =
    match state.processed with
    | [] ->
        { Pending.empty with
            processed = [ img ]
            changes = Matrix.Matrix(255uy, img.Width, img.Height) }
    | prev :: _ ->
        for y in 0 .. img.Height - 1 do
          for x in 0 .. img.Width - 1 do
            if prev.Rep.[y, x] <> img.Rep.[y, x] then state.changes.Rep.[y, x] <- 1uy
        { state with
            processed = img :: state.processed }

  let changes = next.changes |> Contour.extract |> fst

  match changes with
  | [] -> Incomplete next
  | _ ->
      let largest = changes |> List.maxBy (fun c -> c.area)

      match largest.area > next.area with
      | true ->
          Incomplete
            { next with
                area = largest.area
                region = largest.region
                stable = 0 }
      | _ when largest.area < img.Height * img.Width / 3 -> Incomplete { next with stable = 0 }
      | _ when next.stable < 100 -> Incomplete { next with stable = next.stable + 1 }
      | _ ->
          let (top, left), (bottom, right) = next.region
          Complete
            { delayed = next.processed |> List.rev
              region = (top + 2, left + 2), (bottom - 2, right - 2) }
