module Motion

open System.Collections.Generic

type private Window =
  { margin: int
    top: int
    bottom: int
    left: int
    right: int }

let private frame (window: int) (y: int) (x: int) (height: int) (width: int) =
  let margin = window / 2
  { margin = margin
    top = if y < 0 then (margin + 1) - y else (margin + 1)
    bottom = if y > 0 then height - (margin + 2) - y else height - (margin + 2)
    left = if x < 0 then (margin + 1) - x else (margin + 1)
    right = if x > 0 then width - (margin + 2) - x else width - (margin + 2) }

let extract (window: int) (dy: int, dx: int) (prev: Matrix.Matrix<byte>) (cur: Matrix.Matrix<byte>) =
  let buf = Contour.buffer cur
  let mutable next = 0

  let wnd = frame window dy dx cur.Height cur.Width

  let movements =
    new Dictionary<int, Contour.Contour * System.Collections.Generic.List<(int * int)>>()

  for outY in wnd.top .. wnd.bottom do
    for outX in wnd.left .. wnd.right do
      let color = cur.Rep.[outY, outX]
      if prev.Rep.[outY + dy, outX + dx] <> color then
        for inY in outY - wnd.margin .. outY + wnd.margin - 1 do
          for inX in outX - wnd.margin .. outX + wnd.margin - 1 do
            if prev.Rep.[inY + dy, inX + dx] = color then
              let delta = outY - inY, outX - inX

              let deltas =
                match buf.keys.Rep.[outY, outX] with
                | 0 ->
                    next <- next + 1

                    let deltas =
                      new System.Collections.Generic.List<(int * int)>()

                    let extracted = Contour.single buf (outY, outX) next cur
                    movements.Add(next, (extracted, deltas))
                    deltas
                | key -> movements.[key] |> snd

              if buf.edges.Rep.[inY, inX] <> 0 then deltas.Add(delta)

  movements
  |> Seq.where (fun m ->
       let _, deltas = m.Value
       deltas.Count > 0)
  |> Seq.map (fun m ->
       let contour, deltas = m.Value

       let dominant coord =
         deltas
         |> Seq.countBy coord
         |> Seq.maxBy snd
         |> fst

       let delta = dominant fst, dominant snd
       m.Key, contour, delta)
  |> List.ofSeq

let mark (window: int) (dy: int, dx: int) (prev: Matrix.Matrix<byte>) (cur: Matrix.Matrix<byte>) =
  let buf = Contour.buffer cur
  let mutable next = 0

  let w = frame window dy dx cur.Height cur.Width

  let sizes =
    Array.init (cur.Height * cur.Width) (fun _ -> 0)

  let mutable contours = []
  for outY in w.top .. w.bottom do
    for outX in w.left .. w.right do
      let color = cur.Rep.[outY, outX]
      if prev.Rep.[outY + dy, outX + dx] <> color then
        match buf.keys.Rep.[outY, outX] with
        | 0 ->
            next <- next + 1
            contours <-
              (Contour.single buf (outY, outX) next cur)
              :: contours
            sizes.[next] <- 1
        | key -> sizes.[key] <- sizes.[key] + 1

  let output =
    Matrix.Matrix<int>(0, cur.Width, cur.Height)

  let filler (contour: Contour.Contour) =
    if (float32 sizes.[contour.key])
       / (float32 contour.edges.Length) > 0.33f then
      let (sy, sx), (ey, ex) = contour.region
      for y in sy .. ey do
        for x in sx .. ex do
          output.Rep.[y, x] <- output.Rep.[y, x] + 1
    else
      next <- next

  contours |> List.iter filler
  output
