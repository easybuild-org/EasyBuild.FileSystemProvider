module Tests.Utils

open Expecto

module Expect =

    let equal actual expected = Expect.equal actual expected ""

    let notEqual actual expected = Expect.notEqual actual expected ""
