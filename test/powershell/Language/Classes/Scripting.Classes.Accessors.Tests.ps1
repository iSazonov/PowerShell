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
<# Negative tests
    ShouldBeParseError '' IncompleteMemberDefinition 17

        class foo { [string]$bar {} }
        class foo { [string]$bar {get;} }
        class foo { [string]$bar {set;} }
        class foo { [string]$bar {get { 1 };} }
        class foo { [string]$bar {set { 1 };} }
#>

<#
Describe 'Negative Parsing Tests' -Tags "CI" {
    ShouldBeParseError 'class' MissingNameAfterKeyword 5
    ShouldBeParseError 'class foo' MissingTypeBody 9
    ShouldBeParseError 'class foo {' MissingEndCurlyBrace 10
    ShouldBeParseError 'class foo { [int] }' IncompleteMemberDefinition 17
    ShouldBeParseError 'class foo { $private: }' InvalidVariableReference 12
    ShouldBeParseError 'class foo { [int]$global: }' InvalidVariableReference 17
    ShouldBeParseError 'class foo { [Attr()] }' IncompleteMemberDefinition 20
    ShouldBeParseError 'class foo {} class foo {}' MemberAlreadyDefined 13
    ShouldBeParseError 'class foo { $x; $x; }' MemberAlreadyDefined 16 -SkipAndCheckRuntimeError
    ShouldBeParseError 'class foo { [int][string]$x; }' TooManyTypes 17
    ShouldBeParseError 'class foo { [int][string]$x; }' TooManyTypes 17
    ShouldBeParseError 'class foo { static static $x; }' DuplicateQualifier 19
    ShouldBeParseError 'class foo { [zz]$x; }' TypeNotFound 13
    ShouldBeParseError 'class foo { [zz]f() { return 0 } }' TypeNotFound 13
    ShouldBeParseError 'class foo { f([zz]$x) {} }' TypeNotFound 15

    ShouldBeParseError 'class C {} class C {}' MemberAlreadyDefined 11
    ShouldBeParseError 'class C { f(){} f(){} }' MemberAlreadyDefined 16 -SkipAndCheckRuntimeError
    ShouldBeParseError 'class C { F(){} F($o){} [int] F($o) {return 1} }' MemberAlreadyDefined 24 -SkipAndCheckRuntimeError
    ShouldBeParseError 'class C { f(){} f($a){} f(){} }' MemberAlreadyDefined 24 -SkipAndCheckRuntimeError
    ShouldBeParseError 'class C { f([int]$a){} f([int]$b){} }' MemberAlreadyDefined 23 -SkipAndCheckRuntimeError
    ShouldBeParseError 'class C { $x; [int]$x; }' MemberAlreadyDefined 14 -SkipAndCheckRuntimeError
    ShouldBeParseError 'class C { static C($x) {} }' StaticConstructorCantHaveParameters 19 -SkipAndCheckRuntimeError
    ShouldBeParseError 'class C { static C([int]$x = 100) {} }' StaticConstructorCantHaveParameters 19 -SkipAndCheckRuntimeError

    ShouldBeParseError 'class C {f(){ return 1} }' VoidMethodHasReturn 14
    ShouldBeParseError 'class C {[int] f(){ return } }' NonVoidMethodMissingReturnValue 20
    ShouldBeParseError 'class C {[int] f(){} }' MethodHasCodePathNotReturn 15
    ShouldBeParseError 'class C {f(){ $x=1; if($x -lt 2){ return } elseif($x -gt 0 ) {return 1} else{return 2} return 3 } }' @("VoidMethodHasReturn", "VoidMethodHasReturn", "VoidMethodHasReturn") @(62,77,87)

    ShouldBeParseError 'class foo { [int] bar() { $y = $z; return $y} }' VariableNotLocal 31
    ShouldBeParseError 'class foo { bar() { foreach ($zz in $yy) { } } }' VariableNotLocal 36
    ShouldBeParseError 'class foo { bar() { foreach ($zz in $global:yy) { $abc = $zzzzz } } }' VariableNotLocal 57
    ShouldBeParseError 'class foo { bar() { try { $zz = 42 } finally { } $zz } }' VariableNotLocal 49
    ShouldBeParseError 'class foo { bar() { try { $zz = 42 } catch { } $zz } }' VariableNotLocal 47
    ShouldBeParseError 'class foo { bar() { switch (@()) { default { $aa = 42 } } $aa } }' VariableNotLocal 58
    ShouldBeParseError 'class C { $x; static bar() { $this.x = 1 } }' NonStaticMemberAccessInStaticMember 29
    ShouldBeParseError 'class C { $x; static $y = $this.x }' NonStaticMemberAccessInStaticMember 26

    ShouldBeParseError 'class C { $x; static bar() { $this.x = 1 } }' NonStaticMemberAccessInStaticMember 29
    ShouldBeParseError 'class C { $x; static $y = $this.x }' NonStaticMemberAccessInStaticMember 26
    ShouldBeParseError 'class C { $x; static bar() { $This.x = 1 } }' NonStaticMemberAccessInStaticMember 29
    ShouldBeParseError 'class C { $x; static $y = $This.x }' NonStaticMemberAccessInStaticMember 26

    ShouldBeParseError 'class C { [void]foo() { try { throw "foo"} finally { return } } }' ControlLeavingFinally 53
    ShouldBeParseError 'class C { [int]foo() { return; return 1 } }' NonVoidMethodMissingReturnValue 23
    ShouldBeParseError 'class C { [int]foo() { try { throw "foo"} catch { } } }' MethodHasCodePathNotReturn 15
    ShouldBeParseError 'class C { [int]foo() { try { throw "foo"} catch [ArgumentException] {} catch {throw $_} } }' MethodHasCodePathNotReturn 15
    ShouldBeParseError 'class C { [int]foo() { try { throw "foo"} catch [ArgumentException] {return 1} catch {} } }' MethodHasCodePathNotReturn 15
    ShouldBeParseError 'class C { [int]foo() { while ($false) { return 1 } } }' MethodHasCodePathNotReturn 15
    ShouldBeParseError 'class C { [int]foo() { try { mkdir foo } finally { rm -rec foo } } }' MethodHasCodePathNotReturn 15
    ShouldBeParseError 'class C { [int]foo() { try { mkdir foo; return 1 } catch { } } }' MethodHasCodePathNotReturn 15
    ShouldBeParseError 'class C { [bool] Test() { if ($false) { return $true; } } }' MethodHasCodePathNotReturn 17

    ShouldBeParseError 'class C { [int]$i; [void] foo() {$i = 10} }' MissingThis 33
    ShouldBeParseError 'class C { static [int]$i; [void] foo() {$i = 10} }' MissingTypeInStaticPropertyAssignment 40

    ShouldBeParseError 'class C : B' MissingTypeBody 11
}

Describe 'Negative methods Tests' -Tags "CI" {
    ShouldBeParseError 'class foo { f() { param($x) } }' ParamBlockNotAllowedInMethod 18
    ShouldBeParseError 'class foo { f() { dynamicparam {} } }' NamedBlockNotAllowedInMethod 18
    ShouldBeParseError 'class foo { f() { begin {} } }' NamedBlockNotAllowedInMethod 18
    ShouldBeParseError 'class foo { f() { process {} } }' NamedBlockNotAllowedInMethod 18
    ShouldBeParseError 'class foo { f() { end {} } }' NamedBlockNotAllowedInMethod 18
    ShouldBeParseError 'class foo { f([Parameter()]$a) {} }' AttributeNotAllowedOnDeclaration 14
    ShouldBeParseError 'class foo { [int] foo() { return 1 }}' ConstructorCantHaveReturnType 12
    ShouldBeParseError 'class foo { [void] bar($a, [string][int]$b, $c) {} }' MultipleTypeConstraintsOnMethodParam 35
}

Describe 'Negative Assignment Tests' -Tags "CI" {
    ShouldBeParseError 'class foo { [string ]$path; f() { $path="" } }' MissingThis 34
    ShouldBeParseError 'class foo { [string ]$path; f() { [string] $path="" } }' MissingThis 43
    ShouldBeParseError 'class foo { [string ]$path; f() { [int] [string] $path="" } }' MissingThis 49
}

Describe 'Negative Assignment Tests' -Tags "CI" {
    ShouldBeParseError '[DscResource()]class C { [bool] Test() { return $false } [C] Get() { return $this } Set() {} }' DscResourceMissingKeyProperty 0

    # Test method
    ShouldBeParseError '[DscResource()]class C { [DscProperty(Key)][string]$Key; [C] Get() { return $this } Set() {} }' DscResourceMissingTestMethod 0
    ShouldBeParseError '[DscResource()]class C { [DscProperty(Key)][string]$Key; [C] Get() { return $this } Set() {} Test() { } }' DscResourceMissingTestMethod 0
    ShouldBeParseError '[DscResource()]class C { [DscProperty(Key)][string]$Key; [C] Get() { return $this } Set() {} [int] Test() { return 1 } }' DscResourceMissingTestMethod 0
    ShouldBeParseError '[DscResource()]class C { [DscProperty(Key)][string]$Key; [C] Get() { return $this } Set() {} [bool] Test($a) { return $false } }' DscResourceMissingTestMethod 0

    # Get method
    ShouldBeParseError '[DscResource()]class C { [DscProperty(Key)][string]$Key; [bool] Test() { return $false } Set() {} }' DscResourceMissingGetMethod 0
    ShouldBeParseError '[DscResource()]class C { [DscProperty(Key)][string]$Key; [bool] Test() { return $false } Set() {} Get() { } }' DscResourceInvalidGetMethod 98
    ShouldBeParseError '[DscResource()]class C { [DscProperty(Key)][string]$Key; [bool] Test() { return $false } Set() {} [int] Get() { return 1 } }' DscResourceInvalidGetMethod 98
    ShouldBeParseError '[DscResource()]class C { [DscProperty(Key)][string]$Key; [bool] Test() { return $false } Set() {} [C] Get($a) { return $this } }' DscResourceMissingGetMethod 0

    # Set method
    ShouldBeParseError '[DscResource()]class C { [DscProperty(Key)][string]$Key; [bool] Test() { return $false } [C] Get() { return $this } }' DscResourceMissingSetMethod 0
    ShouldBeParseError '[DscResource()]class C { [DscProperty(Key)][string]$Key; [bool] Test() { return $false } [C] Get() { return $this } [int] Set() { return 1 } }' DscResourceMissingSetMethod 0
    ShouldBeParseError '[DscResource()]class C { [DscProperty(Key)][string]$Key; [bool] Test() { return $false } [C] Get() { return $this } Set($a) { } }' DscResourceMissingSetMethod 0

    # Default ctor
    ShouldBeParseError '[DscResource()]class C { [DscProperty(Key)][string]$Key; [bool] Test() { return $false } [C] Get() { return $this } Set() {} C($a) { } }' DscResourceMissingDefaultConstructor 0
}

Describe 'Negative DscResources Tests' -Tags "CI" {
        # Usage errors
        ShouldBeParseError '[Flags()]class C{}' AttributeNotAllowedOnDeclaration 0
        ShouldBeParseError 'class C { [Flags()]$field; }' AttributeNotAllowedOnDeclaration 10
        ShouldBeParseError 'class C { [Flags()]foo(){} }' AttributeNotAllowedOnDeclaration 10

        # Errors related to construction of the attribute
        ShouldBeParseError '[UnknownAttr()]class C{}' CustomAttributeTypeNotFound 1
        ShouldBeParseError '[System.Management.Automation.Cmdlet()]class C{}' MethodCountCouldNotFindBest 0 -SkipAndCheckRuntimeError
        ShouldBeParseError '[System.Management.Automation.Cmdlet("zz")]class C{}' MethodCountCouldNotFindBest 0 -SkipAndCheckRuntimeError
        ShouldBeParseError '[System.Management.Automation.Cmdlet("Get", "Thing", Prop=1)]class C{}' PropertyNotFoundForAttribute 53
        ShouldBeParseError '[System.Management.Automation.Cmdlet("Get", "Thing", ConfirmImpact="foo")]class C{}' CannotConvertValue 67 -SkipAndCheckRuntimeError
        ShouldBeParseError '[System.Management.Automation.Cmdlet("Get", "Thing", NounName="foo")]class C{}' ReadOnlyProperty 53
        ShouldBeParseError '[System.Management.Automation.Cmdlet("Get", "Thing", ConfirmImpact=$zed)]class C{}' ParameterAttributeArgumentNeedsToBeConstant 67
        ShouldBeParseError 'class C{ [ValidateScript({})]$p; }' ParameterAttributeArgumentNeedsToBeConstant 25
}

Describe 'Negative ClassAttributes Tests' -Tags "CI" {
    [System.Management.Automation.Cmdlet("Get", "Thing")]class C{}
    $t = [C].GetCustomAttributes($false)

    It "Should have one attribute" {$t.Count | should be 1}
    It "Should have instance of CmdletAttribute" {$t[0].GetType().FullName | should be System.Management.Automation.CmdletAttribute }

    [System.Management.Automation.CmdletAttribute]$c = $t[0]
    It "Verb should be Get" {$c.VerbName | should be 'Get'}
    It "Noun should be Thing" {$c.NounName | should be 'Thing'}

    [System.Management.Automation.Cmdlet("Get", "Thing", SupportsShouldProcess = $true, SupportsPaging = $true)]class C2{}
    $t = [C2].GetCustomAttributes($false)
    It "Should have one attribute" { $t.Count | should be 1 }
    It "Should have instance of CmdletAttribute" { $t[0].GetType().FullName | should be System.Management.Automation.CmdletAttribute }
    [System.Management.Automation.CmdletAttribute]$c = $t[0]
    It "Verb should be Get" {$c.VerbName | should be 'Get'}
    It "Noun should be Thing" {$c.NounName | should be 'Thing'}

    It  "SupportsShouldProcess should be $true" { $c.ConfirmImpact | should be $true }
    It  "SupportsPaging should be `$true" { $c.SupportsPaging | should be $true }
    Context "Support ConfirmImpact as an attribute" {
        It  "ConfirmImpact should be high" -pending {
            [System.Management.Automation.Cmdlet("Get", "Thing", ConfirmImpact = 'High', SupportsPaging = $true)]class C3{}
            $t = [C3].GetCustomAttributes($false)
            It "Should have one attribute" { $t.Count | should be 1 }
            It "Should have instance of CmdletAttribute" { $t[0].GetType().FullName | should be System.Management.Automation.CmdletAttribute }
            [System.Management.Automation.CmdletAttribute]$c = $t[0]
            $c.ConfirmImpact | should be 'High'

        }
    }
}

Describe 'Property Attributes Test' -Tags "CI" {
        class C { [ValidateSet('a', 'b')]$p; }

        $t = [C].GetProperty('p').GetCustomAttributes($false)
        It "Should have one attribute" { $t.Count | should be 1 }
        [ValidateSet]$v = $t[0]
        It "Should have 2 valid values" { $v.ValidValues.Count | should be 2 }
        It "first value should be a" { $v.ValidValues[0] | should be 'a' }
        It "second value should be b" { $v.ValidValues[1] -eq 'b' }
}

Describe 'Method Attributes Test' -Tags "CI" {
        class C { [Obsolete("aaa")][int]f() { return 1 } }

        $t = [C].GetMethod('f').GetCustomAttributes($false)
        It "Should have one attribute" {$t.Count | should be 1 }
        It "Attribute type should be ObsoleteAttribute" { $t[0].GetType().FullName | should be System.ObsoleteAttribute }
}

Describe 'Positive SelfClass Type As Parameter Test' -Tags "CI" {
        class Point
        {
            Point($x, $y) { $this.x = $x; $this.y = $y }
            Point() {}

            [int] $x = 0
            [int] $y = 0
            Add([Point] $val) {  $this.x += $val.x; $this.y += $val.y;  }

            Print() { Write-Host "[`$x=$($this.x) `$y=$($this.y)]" }
            Set($x, $y) { $this.x = $x; $this.y = $y }
        }
        It  "[Point]::Add works" {
            $point = [Point]::new(100,200)
            $point2 = [Point]::new(1,2)
            $point.Add($point2)

            $point.x | should be 101
            $point.y | should be 202
        }

        It  "[Point]::Add works" {
            $point = New-Object Point 100,200
            $point2 = New-Object Point 1,2
            $point.Add($point2)

            $point.x | should be 101
            $point.y | should be 202
        }
}

Describe 'PositiveReturnSelfClassTypeFromMemberFunction Test' -Tags "CI" {
        class ReturnObjectFromMemberFunctionTest
        {
            [ReturnObjectFromMemberFunctionTest] CreateInstance()
            {
              return [ReturnObjectFromMemberFunctionTest]::new()
            }
            [string] SayHello()
            {
                return "Hello1"
            }
        }
        $f = [ReturnObjectFromMemberFunctionTest]::new()
        $z = $f.CreateInstance() # Line 13
        It "CreateInstance works" { $z.SayHello() | should be 'Hello1' }
}

Describe 'TestMultipleArguments Test' -Tags "CI" {
        if ( $IsCoreCLR ) { $maxCount = 14 } else { $maxCount = 16 }
        for ($i = 0; $i -lt $maxCount; $i++)
        {
            $properties = $(for ($j = 0; $j -le $i; $j++) {
                "        [int]`$Prop$j"
            }) -join "`n"

            $methodParameters = $(for ($j = 0; $j -le $i; $j++) {
                "[int]`$arg$j"
            }) -join ", "

            $ctorAssignments = $(for ($j = 0; $j -le $i; $j++) {
                "            `$this.Prop$j = `$arg$j"
            }) -join "`n"

            $methodReturnValue = $(for ($j = 0; $j -le $i; $j++) {
                "`$arg$j"
            }) -join " + "

            $methodArguments =  $(for ($j = 0; $j -le $i; $j++) {
                $j
            }) -join ", "

            $addUpProperties =  $(for ($j = 0; $j -le $i; $j++) {
                "`$inst.`Prop$j"
            }) -join " + "

            $expectedTotal = (0..$i | Measure-Object -Sum).Sum

            $class = @"
    class Foo
    {
$properties

        Foo($methodParameters)
        {
$ctorAssignments
        }

        [int] DoSomething($methodParameters)
        {
            return $methodReturnValue
        }
    }

    `$inst = [Foo]::new($methodArguments)
    `$sum = $addUpProperties
    It "ExpectedTotal" { `$sum | should be $expectedTotal }
    It "ExpectedTotal"{ `$inst.DoSomething($methodArguments) | should be $expectedTotal }
"@

            Invoke-Expression $class
        }
}

Describe 'Scopes Test' -Tags "CI" {
        class C1
        {
            static C1() {
                $global:foo = $script:foo
            }
            C1() {
                $script:bar = $global:foo
            }
            static [int] f1() {
                return $script:bar + $global:bar
            }
            [int] f2() {
                return $script:bar + $global:bar
            }
        }
}

Describe 'Check PS Class Assembly Test' -Tags "CI" {
        class C1 {}
        $assem = [C1].Assembly
        $attrs = @($assem.GetCustomAttributes($true))
        $expectedAttr = @($attrs | ? { $_  -is [System.Management.Automation.DynamicClassImplementationAssemblyAttribute] })
        It "Expected a DynamicClassImplementationAssembly attribute" { $expectedAttr.Length | should be 1}
}

Describe 'ScriptScopeAccessFromClassMethod' -Tags "CI" {
        Import-Module "$PSScriptRoot\MSFT_778492.psm1"
        try
        {
            $c = Get-MSFT_778492
            It "Method should have found variable in module scope" { $c.F() | should be 'MSFT_778492 script scope'}
        }
        finally
        {
            Remove-Module MSFT_778492
        }
}

Describe 'Hidden Members Test ' -Tags "CI" {
        class C1
        {
            [int]$visibleX
            [int]$visibleY
            hidden [int]$hiddenZ
        }

        # Create an instance
        $instance = [C1]@{ visibleX = 10; visibleY = 12; hiddenZ = 42 }

        It "Access hidden property should still work" { $instance.hiddenZ | should be 42 }


        # Formatting should not include hidden members by default
        $tableOutput = $instance | Format-Table -HideTableHeaders -AutoSize | Out-String
        It "Table formatting should not have included hidden member hiddenZ - should contain 10" { $tableOutput.Contains(10) | should be $true}
        It "Table formatting should not have included hidden member hiddenZ- should contain 12" { $tableOutput.Contains(12) | should be $true}
        It "Table formatting should not have included hidden member hiddenZ - should not contain 42" { $tableOutput.Contains(42) | should be $false}

        # Get-Member should not include hidden members by default
        $member = $instance | Get-Member hiddenZ
        it "Get-Member should not find hidden member w/o -Force" { $member | should be $null }

        # Get-Member should include hidden members with -Force
        $member = $instance | Get-Member hiddenZ -Force
        It "Get-Member should find hidden member w/ -Force" { $member | should not be $null }

        # Tab completion should not return a hidden member
        $line = 'class C2 { hidden [int]$hiddenZ } [C2]::new().h'
        $completions = [System.Management.Automation.CommandCompletion]::CompleteInput($line, $line.Length, $null)
        It "Tab completion should not return a hidden member" { $completions.CompletionMatches.Count | should be 0 }
}

Describe 'BaseMethodCall Test ' -Tags "CI" {
        It "Derived class method call" {"abc".ToString() | should be "abc" }
        # call [object] ToString() method as a base class method.
        It "Base class method call" {([object]"abc").ToString() | should be "System.String" }
}

Describe 'Scoped Types Test' -Tags "CI" {
        class C1 { [string] GetContext() { return "Test scope" } }

        filter f1
        {
            class C1 { [string] GetContext() { return "f1 scope" } }

            return [C1]::new().GetContext()
        }

        filter f2
        {
            class C1 { [string] GetContext() { return "f2 scope" } }

            return (new-object C1).GetContext()
        }

        It "New-Object at test scope" { (new-object C1).GetContext() | should be "Test scope" }
        It "[C1]::new() at test scope" { [C1]::new().GetContext() | should be "Test scope" }

        It "[C1]::new() in nested scope" { (f1) | should be "f1 scope" }
        It "'new-object C1' in nested scope" { (f2) | should be "f2 scope" }


        It "[C1]::new() in nested scope (in pipeline)" { (1 | f1 | f2 | f1) | should be "f1 scope" }
        It "'new-object C1' in nested scope (in pipeline)" { (1 | f2 | f1 | f2) | should be "f2 scope" }
}

Describe 'ParameterOfClassTypeInModule Test' -Tags "CI" {
        try
        {
            $sb = [scriptblock]::Create(@'
enum EE {one = 1}
function test-it([EE]$ee){$ee}
'@)
            $mod = New-Module $sb -Name MSFT_2081529 | Import-Module
            $result = test-it -ee one
            It "Parameter of class/enum type defined in module should work" { $result | should be 1 }
        }
        finally
        {
            Remove-Module -ea ignore MSFT_2081529
        }
}

Describe 'Type building' -Tags "CI" {
    It 'should build the type only once for scriptblock' {
        $a = $null
        1..10 | % {
            class C {}
            if ($a) {
                $a -eq [C] | Should Be $true
            }
            $a = [C]
        }
    }

    It 'should create a new type every time scriptblock executed?' -Pending {
        $sb = [scriptblock]::Create('class A {static [int] $a }; [A]::new()')
        1..2 | % {
        $a = $sb.Invoke()[0]
            ++$a::a | Should Be 1
            ++$a::a | Should Be 2
        }
    }
}

Describe 'RuntimeType created for TypeDefinitionAst' -Tags "CI" {

    It 'can make cast to the right RuntimeType in two different contexts' -pending {

        $ssfe = [System.Management.Automation.Runspaces.SessionStateFunctionEntry]::new("foo", @'
class Base
{
    [int] foo() { return 100 }
}

class Derived : Base
{
    [int] foo() { return 2 * ([Base]$this).foo() }
}

[Derived]::new().foo()
'@)

        $iss = [System.Management.Automation.Runspaces.initialsessionstate]::CreateDefault2()
        $iss.Commands.Add($ssfe)

        $ps = [powershell]::Create($iss)
        $ps.AddCommand("foo").Invoke() | Should be 200
        $ps.Streams.Error | Should Be $null

        $ps1 = [powershell]::Create($iss)
        $ps1.AddCommand("foo").Invoke() | Should be 200
        $ps1.Streams.Error | Should Be $null

        $ps.Commands.Clear()
        $ps.Streams.Error.Clear()
        $ps.AddScript(". foo").Invoke() | Should be 200
        $ps.Streams.Error | Should Be $null
    }
}

Describe 'TypeTable lookups' -Tags "CI" {

    Context 'Call methods from a different thread' {
        $b = [powershell]::Create().AddScript(
@'
class A {}
class B
{
    [object] getA1() { return New-Object A }
    [object] getA2() { return [A]::new() }
}

[B]::new()

'@).Invoke()[0]

        It 'can do type lookup by name' {
            $b.getA1() | Should Be 'A'
        }

        It 'can do type lookup by [type]' {
            $b.getA2() | Should Be 'A'
        }
    }
}

Describe 'Protected method access' -Tags "CI" {

    Add-Type @'
namespace Foo
{
    public class Bar
    {
        protected int x {get; set;}
    }
}
'@

     It 'doesn''t allow protected methods access outside of inheritance chain' -pending {
        $a = [scriptblock]::Create(@'
class A
{
    SetX([Foo.Bar]$bar, [int]$x)
    {
        $bar.x = $x
    }

    [int] GetX([Foo.Bar]$bar)
    {
        Set-StrictMode -Version latest
        return $bar.x
    }
}
[A]::new()

'@).Invoke()
        $bar = [Foo.Bar]::new()
        $throwCount = 0
        try {
            $a.SetX($bar, 42)
        } catch {
            $_.FullyQualifiedErrorId | Should Be PropertyAssignmentException
            $throwCount++
        }
        try {
            $a.GetX($bar)
        } catch {
            $_.FullyQualifiedErrorId | Should Be PropertyNotFoundStrict
            $throwCount++
        }
        $throwCount | Should Be 2
     }

     It 'can call protected methods sequentially from two different contexts' {
        $ssfe = [System.Management.Automation.Runspaces.SessionStateFunctionEntry]::new("foo", @'
class A : Foo.Bar
{
    SetX([int]$x)
    {
        $this.x = $x
    }

    [int] GetX()
    {
        return $this.x
    }
}
return [A]::new()
'@)

        $iss = [System.Management.Automation.Runspaces.initialsessionstate]::CreateDefault()
        $iss.Commands.Add($ssfe)

        $ps = [powershell]::Create($iss)
        $a = $ps.AddCommand("foo").Invoke()[0]
        $ps.Streams.Error | Should Be $null

        $ps1 = [powershell]::Create($iss)
        $a1 = $ps1.AddCommand("foo").Invoke()[0]
        $ps1.Streams.Error | Should Be $null

        $a.SetX(101)
        $a1.SetX(103)

        $a.GetX() | Should Be 101
        $a1.GetX() | Should Be 103
    }
}

Describe 'variable analysis' -Tags "CI" {
    It 'can specify type construct on the local variables' {
        class A { [string] getFoo() { return 'foo'} }

        class B
        {
            static [A] getA ()
            {
                [A] $var = [A]::new()
                return $var
            }
        }

        [B]::getA().getFoo() | Should Be 'foo'
    }
}
#>
