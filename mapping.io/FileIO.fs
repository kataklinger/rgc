module FileIO

#nowarn "9"

open System.IO
open System.Drawing
open System.Drawing.Imaging
open Microsoft.FSharp.NativeInterop

open Matrix

let loadC64Img (path : string) =
    File.ReadAllBytes(path)
    |> Image.rawToC64

let writeImg (path : string) (data : int[,]) =
    let buffer =
        data
        |> Seq.cast<int>
        |> Array.ofSeq

    let h, w = data.GetLength 0, data.GetLength 1
    
    use ptr = fixed buffer 
    use bmp = new Bitmap(w, h, w * 4, PixelFormat.Format32bppRgb, (ptr |> NativePtr.toNativeInt))
    bmp.Save(path, ImageFormat.Png)

let storeRgbImg (path : string) (img : Matrix<int>) =
    img.Rep |> writeImg path

let storeIntensityImg (path : string) (img : Matrix<float32>) =
    img
    |> Image.intToRgbGs
    |> storeRgbImg path

let storeGrayscaleImg (recover : bool) (path : string) (img : Matrix<byte>) =
    let conversion =
        match recover with
        | true -> Image.gsToRgb
        | _ -> Image.gsToRgbGs

    img
    |> conversion
    |> storeRgbImg path

let storeC64Img (path : string) (img : Matrix<byte>) =
    img
    |> Image.c64ToRgb
    |> storeRgbImg path
