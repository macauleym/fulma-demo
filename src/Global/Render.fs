module Render

open Fulma
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Import
open Fable.Core

let pageNotFound =
    Hero.hero [ Hero.IsFullHeight
                Hero.Color IsDanger ]
        [ Hero.body [ ]
            [ Container.container [ Container.Modifiers [ Modifier.TextAlignment(Screen.All, TextAlignment.Centered) ] ]
                [ Heading.h1 [ ]
                    [ str "404" ] ] ] ]

let converter = Showdown.Globals.Converter.Create()

[<Pojo>]
type DangerousInnerHtml =
    { __html : string }

let contentFromMarkdown options str =
    Content.content
        [ yield! options
          yield Content.Props [ DangerouslySetInnerHTML { __html =  converter.makeHtml str } ] ]
        [ ]
