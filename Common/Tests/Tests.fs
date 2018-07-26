module Tests

open NUnit.Framework
open Swensen.Unquote

[<Test>]
let ``should have changed answer``() =
    Library.answer =! 42
