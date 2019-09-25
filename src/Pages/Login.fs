module Login

open Fulma
open Elmish
open Fable.React
open Fable.React.Props
open System
open Helpers
open Fable.Core.JsInterop

let [<Literal>] LoginErrorKey = "login"
let [<Literal>] PasswordErrorKey = "password"
let [<Literal>] SummaryErrorKey = "summary"

type Model =
    {
        Email : string
        Password : string
        Errors : Validation.ErrorDef list
        Waiting : bool
    }

[<RequireQualifiedAccess>]
type SignInResult =
    | Success of Types.Session
    | ValidationFailed of Validation.ErrorDef list
    | Errored of exn

[<RequireQualifiedAccess>]
type ExternalMsg =
    | NoOp
    | SignIn of Types.Session

type Msg =
    | ChangeEmail of string
    | ChangePassword of string
    | Submit
    | SignInResult of SignInResult

module Request =

    let signIn (email : string, password : string) =
        promise {
            let! res = API.User.signIn email password

            match res with
            | Ok user ->
                return SignInResult.Success user

            | Error errors ->
                return SignInResult.ValidationFailed errors
        }

let init () =
    {
        Email = ""
        Password = ""
        Errors = [ ]
        Waiting = false
    }

let update (msg : Msg) (model : Model) =
    match msg with
    | ChangeEmail newValue ->
        { model with
            Email = newValue
            Errors =
                Validation.removeError LoginErrorKey model.Errors
        }
        , Cmd.none
        , ExternalMsg.NoOp

    | ChangePassword newValue ->
        { model with
            Password = newValue
            Errors =
                Validation.removeError PasswordErrorKey model.Errors
        }
        , Cmd.none
        , ExternalMsg.NoOp

    | Submit ->
        { model with
            Waiting = true
        }
        , Cmd.OfPromise.either Request.signIn (model.Email, model.Password) SignInResult (SignInResult.Errored >> SignInResult)
        , ExternalMsg.NoOp

    | SignInResult result ->
        match result with
        | SignInResult.Success user ->
            model
            , Router.MailboxRoute.Inbox None
                |> Router.Mailbox
                |> Router.newUrl
            , ExternalMsg.SignIn user

        | SignInResult.ValidationFailed errors ->
            { model with
                Errors = errors
                Waiting = false
                Password = ""
            }
            , Cmd.none
            , ExternalMsg.NoOp

        | SignInResult.Errored error ->
            Logger.errorfn "[Login] An error occured:\n%A" error

            { model with
                Waiting = false
            }
            , Toast.``something went wrong``
            , ExternalMsg.NoOp


let private viewGlobalErrors errors =
    Validation.tryGetError "summary" errors
    |> Option.map (fun textError ->
        Message.message
            [ Message.Color IsDanger ]
            [
                Message.body [ ]
                    [ str textError ]
            ]
    )
    |> ofOption


let private loginCard model dispatch =
    Card.card
        [
            Props
                [
                    Style
                        [
                            Width "700px"
                            MarginLeft "auto"
                            MarginRight "auto"
                        ]
                ]
            CustomClass "is-transparent"
        ]
        [
            Card.header [ ]
                [
                    Card.Header.title [ Card.Header.Title.Modifiers [ Modifier.TextColor IsGrey ] ]
                        [ str "CONNECTION" ]
                ]
            Card.content [ Props [ Style [ Padding "25px 50px 25px 50px" ] ] ]
                [
                    viewGlobalErrors model.Errors
                    form [ ]
                        [
                            Field.div [ ]
                                [
                                    Label.label [ ]
                                        [ str "Email" ]
                                    Control.div [ ]
                                        [
                                            Input.text
                                                [
                                                    Input.Disabled model.Waiting
                                                    Input.Placeholder "ex: john.doe@domain.com"
                                                    Input.Value model.Email
                                                    Input.Props
                                                        [
                                                            AutoFocus true
                                                            OnInput (fun ev -> !!ev.target?value |> ChangeEmail |> dispatch) ]
                                                        ]
                                        ]
                                    Help.help [ Help.Color IsDanger ]
                                        [ str (Validation.getError LoginErrorKey model.Errors) ]
                                ]

                            Field.div [ ]
                                [
                                    Label.label [ ]
                                        [ str "Password" ]
                                    Control.div [ ]
                                        [
                                            Input.password
                                                [
                                                    Input.Disabled model.Waiting
                                                    Input.Value model.Password
                                                    Input.Props
                                                        [
                                                            OnChange (fun ev -> !!ev.target?value |> ChangePassword |> dispatch)
                                                            OnKeyDown (fun ev -> if ev.keyCode = 13. then Submit |> dispatch)
                                                        ]
                                                ]
                                        ]
                                ]

                            Help.help [ Help.Color IsDanger ]
                                [ str (Validation.getError PasswordErrorKey model.Errors) ]


                            br []

                            Field.div [ ]
                                [
                                    Control.div [ ]
                                        [
                                            Button.list
                                                [
                                                    Button.List.IsCentered
                                                ]
                                                [
                                                    Button.a
                                                        [
                                                            Button.IsText
                                                            // Button.OnClick (fun _ -> dispatch Submit)
                                                            Button.Modifiers [ Modifier.TextTransform TextTransform.UpperCase ]
                                                            Button.Disabled true
                                                        ]
                                                        [ str "Recover my password" ]

                                                    Button.a
                                                        [
                                                            Button.Color IsPrimary
                                                            Button.OnClick (fun _ -> dispatch Submit)
                                                            Button.IsLoading model.Waiting
                                                            Button.Modifiers [ Modifier.TextTransform TextTransform.UpperCase ]
                                                        ]
                                                        [ str "Sign in" ]
                                                ]
                                        ]
                                ]
                            ]
                ]
        ]


let view (model : Model) (dispatch : Dispatch<Msg>) =
    Hero.hero
        [
            Hero.IsFullHeight
            Hero.Color IsPrimary
        ]
        [
            Hero.body [ ]
                [
                    Columns.columns
                        [ Columns.Props [ Style [Width "100%"] ] ]
                        [
                            Column.column
                                [
                                    Column.Width(Screen.All, Column.IsOneThird)
                                    Column.Offset(Screen.All, Column.IsOneThird)
                                    Column.CustomClass "is-one-third is-offset-one-third"
                                ]
                                [
                                    loginCard model dispatch
                                ]
                        ]
                ]
        ]
