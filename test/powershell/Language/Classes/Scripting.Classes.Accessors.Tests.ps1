Import-Module $PSScriptRoot\..\LanguageTestSupport.psm1

Describe "Positive Parse Accessories Tests" -Tags "CI" {
    BeforeAll {
        $testcasesPasitive = @(
            # Simple parse get-set
            @{ name = 'GetSet1'; script = 'class foo1 { [string]$bar {get;set;}}' }
            @{ name = 'GetSet2'; script = 'class foo2 { [string]$bar {get;set}}' }
            @{ name = 'GetSet3'; script = @'
                class foo2 { [string]$bar {get;set}
                            }
'@ }
            @{ name = 'GetSet4'; script = @'
                class foo4 { [string]$bar {get;set
                                        }
                            }
'@ }
            @{ name = 'GetSet5'; script = @'
                class foo5 { [string]$bar {get;set
                                        }
                            }
'@ }
            @{ name = 'GetSet6'; script = @'
                class foo6 { [string]$bar {get;set
                                        }
                            }
'@ }
            @{ name = 'GetSet7'; script = @'
                class foo7 { [string]$bar {
                                            get;set
                                        }
                            }
'@ }
            @{ name = 'GetSet8'; script = @'
                class foo8 { [string]$bar {
                                            get;set;
                                        }
                            }
'@ }
            @{ name = 'GetSet9'; script = @'
                class foo8 { [string]$bar {
                                            get;
                                            set;
                                        }
                            }
'@ }
            @{ name = 'GetSet10'; script = @'
                class foo10 { [string]$bar {
                                            get;
                                            set
                                        }
                            }
'@ }
            @{ name = 'GetSet11'; script = @'
                class foo11 { [string]$bar {
                                            get
                                            set
                                        }
                            }
'@ }
            @{ name = 'GetSet12'; script = @'
                class foo12 { [string]$bar {
                                            get
                                            set
                                        }
                            }
'@ }
            @{ name = 'GetSet13'; script = @'
                class foo13 {
                              [ValidateNotNull()]
                              [string]$bar {
                                            get
                                            set
                                        }
                            }
'@ }
            # Checks: allow blank lines
            @{ name = 'GetSet14'; script = @'
                class foo14 {
                    [int]$pro

                    [string]$bar
                            }
'@ }
            @{ name = 'GetSet15'; script = @'
                class foo15 {
                    [int]$pro;

                    [string]$bar
                            }
'@ }
            # Simple parse 'get-set with initializer'
            # Above we test all options for get-set so no need do all the same for initializer
            # whose code is not dependent on the get-set code
            @{ name = 'Init1'; script = 'class foo1 { [string]$bar {get;set;}=1}' }
            @{ name = 'Init2'; script = @'
                class foo2 { [string]$bar {
                                            get
                                            set
                                        }="init"}
'@ }
            @{ name = 'Init3'; script = @'
                class foo3 { [string]$bar {
                                            get
                                            set
                                        } = "init"
                            }
'@ }
            @{ name = 'Init4'; script = @'
                class foo5 { [string]$bar {
                                            get
                                            set
                                        } =
                                             "init"
                            }
'@ }
            # Simple parse 'methods + properties with accessors'
            @{ name = 'Methods+GetSet1'; script = 'class foo1 { q1(){};[string]$bar{get;set;}}' }
            @{ name = 'Methods+GetSet2'; script = 'class foo2 { q1(){};[string]$bar{get;set;};q2(){}}' }
            @{ name = 'Methods+GetSet3'; script = 'class foo3 { q1(){};[string]$bar{get;set;}=1}' }
            @{ name = 'Methods+GetSet4'; script = 'class foo4 { q1(){};[string]$bar{get;set;}="init";q2(){}}' }
            @{ name = 'Methods+GetSet5'; script = @'
                class foo5 {
                             q1(){}
                             [string]$bar {
                                            get
                                            set
                                        } = "init"
                            }
'@ }
            @{ name = 'Methods+GetSet6'; script = @'
                class foo6 {
                             q1(){};
                             [string]$bar {
                                            get
                                            set
                                        } = "init"
                            }
'@ }
            @{ name = 'Methods+GetSet7'; script = @'
                class foo7 {
                             [string]$bar {
                                            get
                                            set
                                        } = "init"
                             q2(){}
                            }
'@ }
            @{ name = 'Methods+GetSet8'; script = @'
                class foo8 {
                             [string]$bar {
                                            get
                                            set
                                        } = "init";
                             q2(){}
                            }
'@ }
            @{ name = 'Methods+GetSet9'; script = @'
                class foo9 {
                             q1(){}
                             [string]$bar {
                                            get
                                            set
                                        } = "init";q2(){}
                            }
'@ }
            @{ name = 'Methods+GetSet10'; script = @'
                class foo10 {
                             q1(){}
                             [string]$bar {
                                            get
                                            set
                                        } = "init"
                             q2(){}
                            }
'@ }
            @{ name = 'Methods+GetSet11'; script = @'
                class foo11 {
                             q1(){}
                             [string]$bar {
                                            get
                                            set
                                        } = "init"
                             q2(){}
                             [ValidateNotNull()]
                             [string]$bar2 {
                                            get
                                            set
                                        } = "init"
                            }
'@ }

<# Templates
            @{ name = 'GetSet14'; script = @'

'@ }
            @{ name = 'GetSet3'; script = '' }
            @{ name = 'GetSet3'; script = '' }
#>

        ) # End $testcasesPasitive

    } # End BeforeAll

    It "<name>" -TestCases $testcasesPasitive {
        param($script)
        $err = Get-ParseResults $script
        $err.Count | Should Be 0
    }
}

Describe "Accessories works" -Tags "CI" {
    It "Get+Set" {
        class foo
        {
            [string]$bar {
                    set
                    get
                }
        }
        $obj = [foo]::New()
        $obj.bar | Should Be $null
        $obj.bar = 1
        $obj.bar | Should Be "1"
        $obj.bar = "testsstring1"
        $obj.bar | Should Be "testsstring1"
    }

    It "Get+Set+initializer" {
        class foo
        {
            [string]$bar {
                    set
                    get
                } = "testsstring2"
        }
        $obj = [foo]::New()
        $obj.bar | Should Be "testsstring2"
        $obj.bar = 1
        $obj.bar | Should Be "1"
        $obj.bar = "testsstring1"
        $obj.bar | Should Be "testsstring1"
    }

    It "Get{}+Set+`$PSItem" {
        class foo
        {
            [string]$bar {
                    set
                    get { return ">>>$PSItem<<<" }
                }
        }
        $obj = [foo]::New()
        $obj.bar | Should Be ">>><<<"
        $obj.bar = "testsstring1"
        $obj.bar | Should Be ">>>testsstring1<<<"
    }

    It "Get+Set{}+`$PSItem" {
        class foo
        {
            [string]$bar {
                    set { return "!!!$PSItem!!!" }
                    get
                }
        }
        $obj = [foo]::New()
        $obj.bar | Should Be $null
        $obj.bar = "testsstring1"
        $obj.bar | Should Be "!!!testsstring1!!!"
    }

    It "Get{}+Set+`$_" {
        class foo
        {
            [string]$bar {
                    set
                    get { return ">>>$_<<<" }
                }
        }
        $obj = [foo]::New()
        $obj.bar | Should Be ">>><<<"
        $obj.bar = "testsstring1"
        $obj.bar | Should Be ">>>testsstring1<<<"
    }

    It "Get+Set{}+`$_" {
        class foo
        {
            [string]$bar {
                    set { return "!!!$_!!!" }
                    get
                }
        }
        $obj = [foo]::New()
        $obj.bar | Should Be $null
        $obj.bar = "testsstring1"
        $obj.bar | Should Be "!!!testsstring1!!!"
    }

    It "Get{}+Set{}+`$PSItem" {
        class foo
        {
            [string]$bar {
                    set { return "---$PSItem---" }
                    get { return "+++$PSItem+++" }
                }
        }
        $obj = [foo]::New()
        $obj.bar | Should Be "++++++"
        $obj.bar = "testsstring1"
        $obj.bar | Should Be "+++---testsstring1---+++"
    }

    It "Get{}+Set{}+`$PSItem+initializer" {
        class foo
        {
            [string]$bar {
                    set { return "---$PSItem---" }
                    get { return "+++$PSItem+++" }
                } = "qwerty"
        }
        $obj = [foo]::New()
        $obj.bar | Should Be "+++---qwerty---+++"
        $obj.bar = "testsstring1"
        $obj.bar | Should Be "+++---testsstring1---+++"
    }

    It "Get{}+Set{}+`$PSItem+initializer+Static" {
        class foo
        {
            Static
            [string]$bar {
                    set { return "---$PSItem---" }
                    get { return "+++$PSItem+++" }
                } = "qwerty"
        }
        [foo]::bar | Should Be "+++---qwerty---+++"
        [foo]::bar = "testsstring1"
        [foo]::bar | Should Be "+++---testsstring1---+++"
    }

    It "Get{}+Set{}+`$PSItem+Array" {
        class foo
        {
            [array]$bar {
                    set { $var = $PSItem +7; return ,$var }
                    get { return ,($PSItem | Sort-Object) }
                } = @(30,2)
        }
        $obj = [foo]::New()
        $obj.bar | Should Be @(2, 7, 30)
        $obj.bar = @(4, 3, 2, 1)
        $obj.bar | Should Be @(1, 2, 3, 4, 7)
    }

    It "Get{}+Set{}+`$PSItem+Array+Static" {
        class foo
        {
            Static
            [array]$bar {
                    set { $var = $PSItem +7; return ,$var }
                    get { return ,($PSItem | Sort-Object) }
                } = @(10,2)
        }
        [foo]::bar | Should Be @(2, 7, 10)
        [foo]::bar = @(3, 2, 1)
        [foo]::bar | Should Be @(1, 2, 3, 7)
    }

    It "Get{}+Set{}+`$PSItem+Datetime" {
        class foo
        {
            [Datetime]$bar {
                    set { $var = $PSItem.AddDays(1); return $var }
                    get { return $PSItem }
                } = [Datetime]::Today
        }
        $obj = [foo]::New()
        $obj.bar | Should Be ([Datetime]::Today.AddDays(1))
        $obj.bar = [Datetime]::Today.AddYears(5)
        $obj.bar | Should Be ([Datetime]::Today.AddYears(5).AddDays(1))
    }

    It "Get{}+Set{}+`$PSItem+Datetime+Static" {
        class foo
        {
            Static
            [Datetime]$bar {
                    set { $var = $PSItem.AddDays(1); return $var }
                    get { return $PSItem }
                } = [Datetime]::Today
        }
        [foo]::bar | Should Be ([Datetime]::Today.AddDays(1))
        [foo]::bar = [Datetime]::Today.AddYears(5)
        [foo]::bar | Should Be ([Datetime]::Today.AddYears(5).AddDays(1))
    }

    It "Get{}+Set{}+`$PSItem+hashtable" {
        class foo
        {
            [hashtable]$bar {
                    set { return $PSItem }
                    get { return $PSItem }
                } = @{a=1;b=2}
        }
        $obj = [foo]::New()
        $obj.bar.Keys | Should Be (@("a","b"))
        $obj.bar["a"] | Should Be 1
        $obj.bar["b"] | Should Be 2

        $obj.bar = @{a=10;c="teststring"}
        $obj.bar.Keys | Should Be (@("a","c"))
        $obj.bar["a"] | Should Be 10
        $obj.bar["c"] | Should Be "teststring"
    }

    It "Get{}+Set{}+`$PSItem+hashtable+Static" {
        class foo
        {
            Static
            [hashtable]$bar {
                    set { return $PSItem }
                    get { return $PSItem }
                } = @{a=1;b=2}
        }
        [foo]::bar.Keys | Should Be (@("a","b"))
        [foo]::bar["a"] | Should Be 1
        [foo]::bar["b"] | Should Be 2

        [foo]::bar = @{a=10;c="teststring"}
        [foo]::bar.Keys | Should Be (@("a","c"))
        [foo]::bar["a"] | Should Be 10
        [foo]::bar["c"] | Should Be "teststring"
    }

    It "Get{}+Set{}+`$PSItem+PSCustomObject" {
        class foo
        {
            [PSCustomObject]$bar {
                    set { return $PSItem }
                    get { return $PSItem }
                } = [PSCustomObject]@{a=1;b=2}
        }
        $obj = [foo]::New()
        Compare-Object $obj.bar ([PSCustomObject]@{a=1;b=2}) -Property a,b | Should Be $null

        $obj.bar = [PSCustomObject]@{c="teststring";b=20}
        Compare-Object $obj.bar ([PSCustomObject]@{c="teststring";b=20}) -Property a,b | Should Be $null
    }

    It "Get{}+Set{}+`$PSItem+PSCustomObject+Static" {
        class foo
        {
            Static
            [PSCustomObject]$bar {
                    set { return $PSItem }
                    get { return $PSItem }
                } = [PSCustomObject]@{a=1;b=2}
        }
        Compare-Object ([foo]::bar) ([PSCustomObject]@{a=1;b=2}) -Property a,b | Should Be $null

        [foo]::bar = [PSCustomObject]@{c="teststring";b=20}
        Compare-Object ([foo]::bar) ([PSCustomObject]@{c="teststring";b=20}) -Property a,b | Should Be $null
    }

}

Describe 'Negative Parsing Accessories Tests' -Tags "CI" {
    ShouldBeParseError 'class foo { [string]$bar {"NoIdentifier"} }' ClassPropertyAccessorNameNotFound,ClassPropertyAccessorNameNotFound 26,40
    ShouldBeParseError 'class foo { [string]$bar {"NoIdentifier"{}} }' ClassPropertyAccessorNameNotFound,ClassPropertyAccessorNameNotFound 26,42
    ShouldBeParseError 'class foo { [string]$bar {NoGetSetName} }' ClassPropertyAccessorNameNotFound,ClassPropertyAccessorNameNotFound 26,38
    ShouldBeParseError 'class foo { [string]$bar {NoGetSetName{}} }' ClassPropertyAccessorNameNotFound,ClassPropertyAccessorNameNotFound 26,40
    ShouldBeParseError 'class foo { [string]$bar {NoGetSetName{}Set} }' ClassPropertyAccessorNameNotFound 26
    ShouldBeParseError 'class foo { [string]$bar {Set;NoGetSetName} }' ClassPropertyAccessorNameNotFound 30
    ShouldBeParseError 'class foo { [string]$bar {Set{}NoGetSetName} }' ClassPropertyAccessorNameNotFound 31
    ShouldBeParseError 'class foo { [string]$bar {Get{param($a)}Set} }' ClassPropertyAccessorParamBlockFound 30
    ShouldBeParseError 'class foo { [string]$bar {Get{}Set{param($a)}} }' ClassPropertyAccessorParamBlockFound 35
    # Next test strange - check manually to see problem
    ShouldBeParseError 'class foo { [string]$bar {Get;Set' ClassAccessorTerminatorNotFound,MissingEndCurlyBrace,ClassPropertyTerminatorNotFound,MissingEndCurlyBrace 33,26,33,11
    ShouldBeParseError 'class foo { [string]$bar {Get;Set ""}}' ClassAccessorTerminatorNotFound 33
    ShouldBeParseError 'class foo { [string]$bar {Get "";Set}}' ClassAccessorTerminatorNotFound 29
    ShouldBeParseError 'class foo { [string]$bar {Get;Set} = qqqq}' MissingExpressionAfterToken 36
    ShouldBeParseError 'class foo { [string]$bar {Get;Set} =}' MissingExpressionAfterToken 36
    ShouldBeParseError 'class foo { [string]$bar {Get;Set} = 123 qwerty}' ClassPropertyTerminatorNotFound 40
    ShouldBeParseError 'class foo { [string]$bar { get($a){} set } }' ClassAccessorParametersListFound 30
    ShouldBeParseError 'class foo { [string]$bar { set($a){} get } }' ClassAccessorParametersListFound 30
    ShouldBeParseError 'class foo { [string]$bar {set} }' ClassPropertyAccessorNameNotFound 29
    ShouldBeParseError 'class foo { [string]$bar {get} }' ClassPropertyAccessorNameNotFound 29
    ShouldBeParseError 'class foo { [string]$bar {get { 1 }} }' ClassPropertyAccessorNameNotFound 35
    ShouldBeParseError 'class foo { [string]$bar {set { 1 }} }' ClassPropertyAccessorNameNotFound 35
    ShouldBeParseError 'class foo { [string]$bar {} }' ClassPropertyAccessorNameNotFound 26
}
