﻿// ${CHAR:Backspace}
module Module

type SomeRecord =
   { field1: string
     field2: string

     {caret}[<SomeAttribute1>]
     [<SomeAttribute2>]
     field3: string }
