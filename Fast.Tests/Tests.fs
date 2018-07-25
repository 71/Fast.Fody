module TestIntegrity

open Swensen.Unquote
open NUnit.Framework

[<Test>]
let ``should replace answer property body``() =
    Library.answer =! "Forty-two."
