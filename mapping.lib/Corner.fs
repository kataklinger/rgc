module Corner

let extract (original: Matrix.Matrix<byte>) (blured: Matrix.Matrix<byte>) =
  let output =
    Matrix.Matrix(false, blured.Width, blured.Height)

  let mutable corners = List.empty

  for outY in 4 .. blured.Height - 5 do
    for outX in 4 .. blured.Width - 5 do
      let color = blured.Rep.[outY, outX]
      let mutable count = 0
      let mutable pattern = 0xffuy

      let update value clear =
        match value with
        | v when v = color -> count <- count + 1
        | _ -> pattern <- pattern &&& ~~~clear

      update blured.Rep.[outY - 1, outX - 1] (0x01uy ||| 0x40uy ||| 0x80uy)
      update blured.Rep.[outY - 1, outX] (0x01uy ||| 0x02uy ||| 0x80uy)
      update blured.Rep.[outY - 1, outX + 1] (0x01uy ||| 0x02uy ||| 0x04uy)
      update blured.Rep.[outY, outX - 1] (0x20uy ||| 0x40uy ||| 0x80uy)
      update blured.Rep.[outY, outX + 1] (0x02uy ||| 0x04uy ||| 0x08uy)
      update blured.Rep.[outY + 1, outX - 1] (0x10uy ||| 0x20uy ||| 0x40uy)
      update blured.Rep.[outY + 1, outX] (0x08uy ||| 0x10uy ||| 0x20uy)
      update blured.Rep.[outY + 1, outX + 1] (0x04uy ||| 0x08uy ||| 0x10uy)

      let mutable encoded = bigint (int original.Rep.[outY, outX])

      if count = 3 then
        count <- 0
        while pattern <> 0uy do
          pattern <- pattern &&& (pattern - 1uy)
          count <- count + 1
        if count = 1 then
          let mutable close = false
          for inY in outY - 2 .. outY + 2 do
            for inX in outX - 2 .. outX + 2 do
              encoded <-
                (encoded <<< 4)
                ||| (bigint (int original.Rep.[inY, inX]))
              close <- close || output.Rep.[inY, inX]

          if not close then
            output.Rep.[outY, outX] <- true

            let feature =
              Feature.Handle(encoded, { area = 25; position = (outY, outX) })

            corners <- feature :: corners
  corners
