module Print

type Cell =
  { histogram: float32 []
    intensity: float32
    samples: int }

type Segment =
  { position: Primitive.Point
    image: Matrix.Matrix<byte>
    motion: Matrix.Matrix<int> }

module Cell =
  let empty () =
    { histogram = Array.init 16 (fun _ -> 0.f)
      intensity = 0.1f
      samples = 0 }

let private emptyCellInit _ _ = Cell.empty ()

type Print =
  { image: Matrix.Matrix<Cell>
    beginning: int * int }

let empty =
  { image = Matrix.Matrix<Cell>(0, 0, emptyCellInit)
    beginning = 0, 0 }

let update (map: Print) (seg: Segment) =
  let y, x = seg.position

  let top, left = map.beginning

  let bottom, right =
    top + map.image.Height, left + map.image.Width

  let topAdj = if y < top then seg.image.Height else 0

  let bottomAdj =
    if y + seg.image.Height > bottom then seg.image.Height else 0

  let leftAdj = if x < left then seg.image.Width else 0

  let rightAdj =
    if x + seg.image.Width > right then seg.image.Width else 0

  let beginTop = top - topAdj
  let beginLeft = left - leftAdj

  let image =
    match topAdj
          <> 0
          || leftAdj <> 0
          || bottomAdj <> 0
          || rightAdj <> 0 with
    | true ->
        map.image
        |> Matrix.frameInit emptyCellInit topAdj leftAdj bottomAdj rightAdj
    | _ -> map.image

  for dy in 0 .. seg.image.Height - 1 do
    for dx in 0 .. seg.image.Width - 1 do
      let prev =
        image.Rep.[y - beginTop + dy, x - beginLeft + dx]

      let intensity =
        match seg.motion.Rep.[dy, dx] with
        | 0 -> max 1.f (prev.intensity + 0.05f)
        | _ -> 0.05f

      let next =
        { prev with
            intensity = intensity
            samples = prev.samples + 1 }

      next.histogram.[(int seg.image.Rep.[dy, dx])] <- next.histogram.[(int seg.image.Rep.[dy, dx])]
                                                       + next.intensity
      image.Rep.[y - beginTop + dy, x - beginLeft + dx] <- next

  { image = image
    beginning = beginTop, beginLeft }

let private blur (img: Matrix.Matrix<Cell>) =
  let kernel = Filter.gaussian 1.f
  let margin = kernel.Width / 2

  let output =
    Matrix.Matrix<Cell>(img.Width, img.Height, emptyCellInit)

  for outY in margin .. img.Height - margin - 1 do
    for outX in margin .. img.Width - margin - 1 do
      let hist = Array.init 16 (fun _ -> 0.f)
      let src = img.Rep.[outY, outX].histogram

      for inY in 0 .. kernel.Width - 1 do
        for inX in 0 .. kernel.Width - 1 do
          for z in 0 .. hist.Length - 1 do
            if src.[z] > 0.f then
              hist.[z] <- hist.[z]
                          + kernel.Rep.[inY, inX]
                          * img.Rep.[outY + inY - margin, outX + inX - margin].histogram.[z]

      output.Rep.[outY, outX] <- { output.Rep.[outY, outX] with
                                     histogram = hist }
  output

let private convert =
  Matrix.map (fun c ->
    c.histogram
    |> Array.mapi (fun i v -> i, v)
    |> Array.maxBy snd
    |> fst
    |> byte)

let plot (map: Print) =
  let normalize (arr: float32 []) =
    let total = arr |> Array.sum

    arr |> Array.map ((*) (1.0f / total))

  map.image
  |> Matrix.map (fun c ->
       { c with
           histogram = normalize c.histogram })
  |> blur
  |> convert

let quickPlot (map: Print) = map.image |> convert
