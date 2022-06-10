using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Solnet.Anchor.CodeGen;
using Solnet.Anchor.Models;
using Solnet.Anchor.Models.Accounts;
using Solnet.Anchor.Models.Types;
using Solnet.Anchor.Models.Types.Base;
using Solnet.Anchor.Models.Types.Enum;
using Solnet.Wallet.Utilities;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solnet.Anchor
{
    public static class ClientGeneratorDefaultValues
    {
        public static readonly AccessorListSyntax PropertyAccessorList = AccessorList(List<AccessorDeclarationSyntax>()
            .Add(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken)))
            .Add(AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken))));

        public static readonly AccessorListSyntax ClientPropertyAccessorList = AccessorList(List<AccessorDeclarationSyntax>()
            .Add(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken)))
            .Add(AccessorDeclaration(SyntaxKind.InitAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken))));


        public static readonly SyntaxTokenList PublicModifier = TokenList(Token(SyntaxKind.PublicKeyword));

        public static readonly SyntaxTokenList PublicStaticModifiers = TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword));

        public static readonly SyntaxTokenList PublicPartialModifiers = TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.PartialKeyword));

        public static readonly SyntaxTokenList PublicAwaitModifiers = TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.AsyncKeyword));

        public static readonly SyntaxToken OpenBraceToken = Token(SyntaxKind.OpenBraceToken);

    }

    public class ClientGenerator
    {
        private static List<string> LANG_KEYWORDS;


        static ClientGenerator()
        {
            LANG_KEYWORDS = new()
            {
                "params"
            };
        }


        public SyntaxTree GenerateSyntaxTree(Idl idl)
        {
            List<MemberDeclarationSyntax> members = new();

            if (idl.Accounts != null && idl.Accounts.Length > 0)
                members.Add(GenerateAccountsSyntaxTree(idl));

            members.Add(GenerateErrorsSyntaxTree(idl));

            if (idl.Events != null && idl.Events.Length > 0)
                members.Add(GenerateEventsSyntaxTree(idl));

            if (idl.Types != null && idl.Types.Length > 0)
                members.Add(GenerateTypesSyntaxTree(idl));

            members.Add(GenerateClientSyntaxTree(idl));

            members.Add(GenerateProgramSyntaxTree(idl));

            var namespaceDeclaration = NamespaceDeclaration(ParseName(idl.Name.ToPascalCase()), List<ExternAliasDirectiveSyntax>(), List<UsingDirectiveSyntax>(), List(members));

            var st = SyntaxTree(CompilationUnit(List<ExternAliasDirectiveSyntax>(), List(GenerateUsings(idl)), List<AttributeListSyntax>(), SingletonList<MemberDeclarationSyntax>(namespaceDeclaration)));

            return st;
        }

        public string GenerateCode(Idl idl)
        {
            return GenerateSyntaxTree(idl).GetRoot().NormalizeWhitespace().ToFullString();
        }

        private List<UsingDirectiveSyntax> GenerateUsings(Idl idl)
        {
            List<UsingDirectiveSyntax> usings = new()
            {
                UsingDirective(IdentifierName("System")),
                UsingDirective(IdentifierName("System.Collections.Generic")),
                UsingDirective(IdentifierName("System.Linq")),
                UsingDirective(IdentifierName("System.Numerics")),
                UsingDirective(IdentifierName("System.Threading.Tasks")),
                UsingDirective(IdentifierName("Solnet")),
                UsingDirective(IdentifierName("Solnet.Programs.Abstract")),
                UsingDirective(IdentifierName("Solnet.Programs.Utilities")),
                UsingDirective(IdentifierName("Solnet.Rpc")),
                UsingDirective(IdentifierName("Solnet.Rpc.Builders")),
                UsingDirective(IdentifierName("Solnet.Rpc.Core.Http")),
                UsingDirective(IdentifierName("Solnet.Rpc.Core.Sockets")),
                UsingDirective(IdentifierName("Solnet.Rpc.Types")),
                UsingDirective(IdentifierName("Solnet.Wallet")),
                UsingDirective(IdentifierName(idl.Name.ToPascalCase())),
                UsingDirective(IdentifierName(idl.Name.ToPascalCase() + ".Program")),
                UsingDirective(IdentifierName(idl.Name.ToPascalCase() + ".Errors"))
            };

            if (idl.Accounts != null && idl.Accounts.Length > 0)
                usings.Add(UsingDirective(IdentifierName(idl.Name.ToPascalCase() + ".Accounts")));

            if (idl.Events != null && idl.Events.Length > 0)
                usings.Add(UsingDirective(IdentifierName(idl.Name.ToPascalCase() + ".Events")));

            if (idl.Types != null && idl.Types.Length > 0)
                usings.Add(UsingDirective(IdentifierName(idl.Name.ToPascalCase() + ".Types")));



            return usings;
        }

        private MemberDeclarationSyntax GenerateProgramSyntaxTree(Idl idl)
        {
            List<MemberDeclarationSyntax> classes = new();
            List<MemberDeclarationSyntax> instructions = new();

            foreach (var instr in idl.Instructions)
            {
                classes.AddRange(GenerateAccountsClassSyntaxTree(instr.Accounts, instr.Name.ToPascalCase()));
                instructions.Add(GenerateInstructionSerializationSyntaxTree(idl, instr));
            }

            classes.Add(ClassDeclaration(List<AttributeListSyntax>(), ClientGeneratorDefaultValues.PublicStaticModifiers, Identifier(idl.Name.ToPascalCase() + "Program"), null, null, List<TypeParameterConstraintClauseSyntax>(), List(instructions)));


            return NamespaceDeclaration(IdentifierName("Program"), List<ExternAliasDirectiveSyntax>(), List<UsingDirectiveSyntax>(), List(classes));
        }

        private List<ExpressionSyntax> GenerateKeysInitExpressions(IIdlAccountItem[] accounts, ExpressionSyntax identifierNameSyntax)
        {
            List<ExpressionSyntax> initExpressions = new();


            foreach (var acc in accounts)
            {
                if (acc is IdlAccounts mulAccs)
                {
                    initExpressions.AddRange(GenerateKeysInitExpressions(
                        mulAccs.Accounts,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            identifierNameSyntax,
                            IdentifierName(mulAccs.Name.ToPascalCase()))));
                }
                else if (acc is IdlAccount singleAcc)
                {


                    initExpressions.Add(InvocationExpression(
                        MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        QualifiedName(QualifiedName(QualifiedName(IdentifierName("Solnet"), IdentifierName("Rpc")),
                        IdentifierName("Models")),
                IdentifierName("AccountMeta")),
                        IdentifierName(singleAcc.IsMut ? "Writable" : "ReadOnly")),
                    ArgumentList(SeparatedList(new ArgumentSyntax[]
                    {
                        Argument(MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            identifierNameSyntax,
                            IdentifierName(singleAcc.Name.ToPascalCase()))),
                        Argument(LiteralExpression(singleAcc.IsSigner ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression))
                    }))));

                }
            }
            return initExpressions;
        }

        private MemberDeclarationSyntax GenerateInstructionSerializationSyntaxTree(Idl idl, IdlInstruction instr)
        {
            List<ParameterSyntax> parameters = new();
            IIdlTypeDefinitionTy[] definedTypes = idl.Types;

            EqualsValueClauseSyntax defaultProgram = string.IsNullOrEmpty(idl.DefaultProgramAddress) ? null :
                EqualsValueClause(LiteralExpression(SyntaxKind.NullLiteralExpression));

            parameters.Add(Parameter(List<AttributeListSyntax>(), TokenList(), IdentifierName(instr.Name.ToPascalCase() + "Accounts"), Identifier("accounts"), null));

            foreach (var arg in instr.Args)
            {
                parameters.Add(Parameter(List<AttributeListSyntax>(), TokenList(), GetTypeSyntax(arg.Type), Identifier(GetNormalizedName(arg.Name)), null));
            }

            parameters.Add(Parameter(List<AttributeListSyntax>(), TokenList(), IdentifierName("PublicKey"), Identifier("programId"), defaultProgram));

            List<ExpressionSyntax> initExprs = GenerateKeysInitExpressions(instr.Accounts, IdentifierName("accounts"));

            List<StatementSyntax> body = new();

            if (!string.IsNullOrEmpty(idl.DefaultProgramAddress))
            {
                var pkInit = ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.CoalesceAssignmentExpression,
                    IdentifierName("programId"),
                    ImplicitObjectCreationExpression(
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        Literal(idl.DefaultProgramAddress))))), default)));
                body.Add(pkInit);
            }


            var initExpr = InitializerExpression(SyntaxKind.CollectionInitializerExpression, ClientGeneratorDefaultValues.OpenBraceToken, SeparatedList<SyntaxNode>(initExprs), Token(SyntaxKind.CloseBraceToken));

            body.Add(LocalDeclarationStatement(VariableDeclaration(
                GenericName(Identifier("List"), TypeArgumentList(SeparatedList(new TypeSyntax[] { QualifiedName(QualifiedName(QualifiedName(IdentifierName("Solnet"), IdentifierName("Rpc")),
                        IdentifierName("Models")),
                IdentifierName("AccountMeta")) }))),
                SingletonSeparatedList(VariableDeclarator(Identifier("keys"), null,
                EqualsValueClause(ImplicitObjectCreationExpression(ArgumentList(), initExpr)))))));

            body.Add(LocalDeclarationStatement(VariableDeclaration(
                ArrayType(PredefinedType(Token(SyntaxKind.ByteKeyword)), SingletonList(ArrayRankSpecifier())),
                SingletonSeparatedList(VariableDeclarator(Identifier("_data"),
                    null,
                    EqualsValueClause(ArrayCreationExpression(
                        ArrayType(PredefinedType(Token(SyntaxKind.ByteKeyword)),
                            SingletonList(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(
                                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1200)))))))))))));

            body.Add(LocalDeclarationStatement(VariableDeclaration(PredefinedType(Token(SyntaxKind.IntKeyword)),
                SingletonSeparatedList(VariableDeclarator(Identifier("offset"), null,
                EqualsValueClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))))))));


            body.Add(ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("_data"), IdentifierName("WriteU64")),
                ArgumentList(SeparatedList(new ArgumentSyntax[]
                {
                    Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(SigHash.GetInstructionSignatureHash(instr.Name, "global")))),
                    Argument(IdentifierName("offset"))
                })))));

            body.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.AddAssignmentExpression, IdentifierName("offset"),
                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(8)))));


            foreach (var arg in instr.Args)
            {
                body.AddRange(GenerateArgSerializationSyntaxList(definedTypes, arg.Type, IdentifierName(GetNormalizedName(arg.Name))));
            }

            body.Add(LocalDeclarationStatement(VariableDeclaration(
                ArrayType(PredefinedType(Token(SyntaxKind.ByteKeyword)), SingletonList(ArrayRankSpecifier())),
                SingletonSeparatedList(VariableDeclarator(Identifier("resultData"),
                    null,
                    EqualsValueClause(ArrayCreationExpression(
                        ArrayType(PredefinedType(Token(SyntaxKind.ByteKeyword)),
                            SingletonList(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(IdentifierName("offset"))))))))))));

            body.Add(ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("Array"), IdentifierName("Copy")),
                ArgumentList(SeparatedList(new ArgumentSyntax[]
                {
                    Argument(IdentifierName("_data")),
                    Argument(IdentifierName("resultData")),
                    Argument(IdentifierName("offset"))
                })))));

            body.Add(ReturnStatement(ObjectCreationExpression(QualifiedName(QualifiedName(QualifiedName(IdentifierName("Solnet"), IdentifierName("Rpc")),
                        IdentifierName("Models")),
                IdentifierName("TransactionInstruction")), null,
                InitializerExpression(SyntaxKind.ObjectInitializerExpression, SeparatedList(new ExpressionSyntax[]
                {
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName("Keys"), IdentifierName("keys") ),
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName("ProgramId"),
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("programId"), IdentifierName("KeyBytes"))),

                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName("Data"), IdentifierName("resultData")),

                })))));

            return MethodDeclaration(List<AttributeListSyntax>(), ClientGeneratorDefaultValues.PublicStaticModifiers, QualifiedName(QualifiedName(QualifiedName(IdentifierName("Solnet"), IdentifierName("Rpc")),
                        IdentifierName("Models")),
                IdentifierName("TransactionInstruction")), null, Identifier(instr.Name.ToPascalCase()), null, ParameterList(SeparatedList(parameters)), List<TypeParameterConstraintClauseSyntax>(), Block(body), null);
        }

        private bool IsSimpleEnum(IIdlTypeDefinitionTy[] types, string name)
        {
            var res = types.FirstOrDefault(x => x.Name == name);

            if (res is EnumIdlTypeDefinition enumDef)
            {
                return enumDef.Variants.All(x => x is SimpleEnumVariant);
            }
            return false;
        }

        private IEnumerable<StatementSyntax> GenerateDeserializationSyntaxList(IIdlTypeDefinitionTy[] definedTypes, IIdlType type, ExpressionSyntax identifierNameSyntax)
        {
            List<StatementSyntax> syntaxes = new();

            if (type is IdlDefined definedType)
            {
                if (!IsSimpleEnum(definedTypes, definedType.TypeName))
                {
                    var tmpName = identifierNameSyntax.ToString().ToCamelCase().ToCamelCase().Replace(".", null).Replace("[", null).Replace("]", null);
                    //tipo.Deserialize(_data, offset, out var bla);
                    //dest.prop = bla;
                    syntaxes.Add(ExpressionStatement(AssignmentExpression(
                        SyntaxKind.AddAssignmentExpression,
                        IdentifierName("offset"),
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(definedType.TypeName),
                                IdentifierName("Deserialize")),
                            ArgumentList(SeparatedList(new ArgumentSyntax[]
                            {
                                Argument(IdentifierName("_data")),
                                Argument(IdentifierName("offset")),
                                Argument(null, Token(SyntaxKind.OutKeyword), DeclarationExpression(IdentifierName("var"), SingleVariableDesignation(Identifier(tmpName))))

                            }))))));

                    syntaxes.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, identifierNameSyntax, IdentifierName(tmpName))));
                }
                else
                {
                    syntaxes.Add(ExpressionStatement(AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        identifierNameSyntax,
                        CastExpression(IdentifierName(definedType.TypeName),
                            InvocationExpression(MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("_data"),
                                IdentifierName("GetU8")),
                            ArgumentList(SeparatedList(new ArgumentSyntax[]
                            {
                                Argument(IdentifierName("offset"))
                            })))))));

                    syntaxes.Add(ExpressionStatement(AssignmentExpression(
                        SyntaxKind.AddAssignmentExpression,
                        IdentifierName("offset"),
                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))));
                }
            }
            else if (type is IdlValueType valueType)
            {
                var (serializerFunctionName, typeSize) = GetDeserializationValuesForValueType(valueType);

                syntaxes.Add(ExpressionStatement(AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        identifierNameSyntax,
                        InvocationExpression(MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("_data"),
                            serializerFunctionName),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                            Argument(IdentifierName("offset"))
                        }))))));

                syntaxes.Add(ExpressionStatement(AssignmentExpression(
                    SyntaxKind.AddAssignmentExpression,
                    IdentifierName("offset"),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(typeSize)))));
            }
            else if (type is IdlString str)
            {
                var tmpName = identifierNameSyntax.ToString().ToCamelCase().ToCamelCase().Replace(".", null).Replace("[", null).Replace("]", null);

                syntaxes.Add(ExpressionStatement(AssignmentExpression(
                    SyntaxKind.AddAssignmentExpression,
                    IdentifierName("offset"),
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("_data"),
                            IdentifierName("GetBorshString")),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                                Argument(IdentifierName("offset")),
                                Argument(null, Token(SyntaxKind.OutKeyword), DeclarationExpression(IdentifierName("var"), SingleVariableDesignation(Identifier(tmpName))))

                        }))))));

                syntaxes.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, identifierNameSyntax, IdentifierName(tmpName))));
            }
            else if (type is IdlArray arr)
            {
                var lenIdentifier = Identifier(identifierNameSyntax.ToString().ToCamelCase().Replace(".", null).Replace("[", null).Replace("]", null) + "Length");
                var lenIdExpression = IdentifierName(lenIdentifier);

                ExpressionSyntax lenExpression = arr.Size.HasValue ?
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(arr.Size.Value)) :
                    lenIdExpression;

                if (!arr.Size.HasValue)
                {
                    syntaxes.Add(LocalDeclarationStatement(VariableDeclaration(PredefinedType(Token(SyntaxKind.UIntKeyword)),
                    SingletonSeparatedList(VariableDeclarator(lenIdentifier, null,
                    EqualsValueClause(InvocationExpression(MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("_data"),
                        IdentifierName("GetU32")),
                    ArgumentList(SeparatedList(new ArgumentSyntax[]
                    {
                        Argument(IdentifierName("offset"))
                    })))))))));

                    syntaxes.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.AddAssignmentExpression, IdentifierName("offset"),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(4)))));


                }

                if (arr.ValuesType is IdlValueType innerType && (innerType.TypeName == "u8"))
                {
                    syntaxes.Add(ExpressionStatement(AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    identifierNameSyntax,
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("_data"),
                            IdentifierName("GetBytes")),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                                Argument(IdentifierName("offset")),
                                Argument(lenExpression)

                        }))))));

                    syntaxes.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.AddAssignmentExpression, IdentifierName("offset"), lenExpression)));
                }
                else
                {
                    var tmp = identifierNameSyntax.ToString().ToCamelCase().Replace(".", null).Replace("[", null).Replace("]", null) + "Idx";
                    var idxToken = Identifier(tmp);
                    var idxExpression = IdentifierName(idxToken);

                    var iteratorIdxDeclaration = VariableDeclaration(PredefinedType(Token(SyntaxKind.UIntKeyword)), SingletonSeparatedList(VariableDeclarator(idxToken, null,
                    EqualsValueClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))))));

                    var condition = BinaryExpression(SyntaxKind.LessThanExpression, idxExpression, lenExpression);

                    var increment = SingletonSeparatedList<ExpressionSyntax>(PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, idxExpression));

                    var typeSyntax = (ArrayTypeSyntax)GetTypeSyntax(type);

                    syntaxes.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, identifierNameSyntax, ArrayCreationExpression(FixArrayCreationSize(typeSyntax, lenExpression)))));

                    var elementAccessExpression = ElementAccessExpression(identifierNameSyntax, BracketedArgumentList(SingletonSeparatedList(Argument(idxExpression))));

                    syntaxes.Add(ForStatement(iteratorIdxDeclaration, SeparatedList<ExpressionSyntax>(), condition, increment, Block(GenerateDeserializationSyntaxList(definedTypes, arr.ValuesType, elementAccessExpression))));
                }

            }
            else if (type is IdlBigInt bi)
            {
                bool isSigned = bi.TypeName == "i128";

                syntaxes.Add(ExpressionStatement(AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        identifierNameSyntax,
                        InvocationExpression(MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("_data"),
                            IdentifierName("GetBigInt")),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                            Argument(IdentifierName("offset")),
                            Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(16))),
                            Argument(LiteralExpression(isSigned ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression))
                        }))))));

                syntaxes.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.AddAssignmentExpression, IdentifierName("offset"),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(16)))));
            }
            else if (type is IdlPublicKey)
            {
                syntaxes.Add(ExpressionStatement(AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        identifierNameSyntax,
                        InvocationExpression(MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("_data"),
                            IdentifierName("GetPubKey")),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                                Argument(IdentifierName("offset"))
                        }))))));

                syntaxes.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.AddAssignmentExpression, IdentifierName("offset"),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(32)))));
            }
            else if (type is IdlOptional optionalType)
            {
                var cond = InvocationExpression(MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("_data"),
                            IdentifierName("GetBool")),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                                Argument(PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, IdentifierName("offset")))
                        })));

                syntaxes.Add(IfStatement(
                    cond,
                    Block(GenerateDeserializationSyntaxList(definedTypes, optionalType.ValuesType, identifierNameSyntax))));
            }
            else
            {
                throw new Exception("Unexpected type " + type.GetType().FullName);
            }

            return syntaxes;
        }


        private ArrayTypeSyntax FixArrayCreationSize(ArrayTypeSyntax array, ExpressionSyntax rankExpression)
            => array.ElementType switch
            {
                ArrayTypeSyntax innerArray => array.WithElementType(FixArrayCreationSize(innerArray, rankExpression)),
                _ => array.WithRankSpecifiers(SingletonList(ArrayRankSpecifier(SingletonSeparatedList(rankExpression))))
            };


        private IEnumerable<StatementSyntax> GenerateArgSerializationSyntaxList(IIdlTypeDefinitionTy[] definedTypes, IIdlType type, ExpressionSyntax identifierNameSyntax)
        {
            List<StatementSyntax> syntaxes = new();

            if (type is IdlDefined definedType)
            {
                if (!IsSimpleEnum(definedTypes, definedType.TypeName))
                {
                    syntaxes.Add(ExpressionStatement(AssignmentExpression(
                        SyntaxKind.AddAssignmentExpression,
                        IdentifierName("offset"), InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                identifierNameSyntax,
                                IdentifierName("Serialize")),
                            ArgumentList(SeparatedList(new ArgumentSyntax[]
                            {
                            Argument(IdentifierName("_data")),
                            Argument(IdentifierName("offset"))
                            }))))));
                }
                else
                {
                    syntaxes.Add(ExpressionStatement(InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("_data"),
                            IdentifierName("WriteU8")),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                        Argument(CastExpression(PredefinedType(Token(SyntaxKind.ByteKeyword)), identifierNameSyntax)),
                        Argument(IdentifierName("offset"))
                        })))));

                    syntaxes.Add(ExpressionStatement(AssignmentExpression(
                        SyntaxKind.AddAssignmentExpression,
                        IdentifierName("offset"),
                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))));
                }
            }
            else if (type is IdlValueType valueType)
            {
                var (serializerFunctionName, typeSize) = GetSerializationValuesForValueType(valueType);

                syntaxes.Add(ExpressionStatement(InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("_data"),
                        serializerFunctionName),
                    ArgumentList(SeparatedList(new ArgumentSyntax[]
                    {
                        Argument(identifierNameSyntax),
                        Argument(IdentifierName("offset"))
                    })))));

                syntaxes.Add(ExpressionStatement(AssignmentExpression(
                    SyntaxKind.AddAssignmentExpression,
                    IdentifierName("offset"),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(typeSize)))));
            }
            else if (type is IdlString str)
            {
                syntaxes.Add(ExpressionStatement(AssignmentExpression(
                    SyntaxKind.AddAssignmentExpression,
                    IdentifierName("offset"),
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("_data"),
                            IdentifierName("WriteBorshString")),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                            Argument(identifierNameSyntax),
                            Argument(IdentifierName("offset"))
                        }))))));
            }
            else if (type is IdlArray arr)
            {
                var varIdIdentifier = Identifier(identifierNameSyntax.ToString().ToCamelCase() + "Element");
                var varIdExpression = IdentifierName(varIdIdentifier);

                if (!arr.Size.HasValue)
                {
                    syntaxes.Add(ExpressionStatement(InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("_data"),
                            IdentifierName("WriteS32")),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                            Argument(MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                identifierNameSyntax,
                                IdentifierName("Length"))),
                            Argument(IdentifierName("offset"))
                        })))));

                    syntaxes.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.AddAssignmentExpression, IdentifierName("offset"),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(4)))));
                }

                // need to create different serialization for u8 arr

                if (arr.ValuesType is IdlValueType innerType && (innerType.TypeName == "u8"))
                {
                    syntaxes.Add(ExpressionStatement(InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("_data"),
                            IdentifierName("WriteSpan")),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                            Argument(identifierNameSyntax),
                            Argument(IdentifierName("offset"))
                        })))));

                    syntaxes.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.AddAssignmentExpression, IdentifierName("offset"),
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, identifierNameSyntax, IdentifierName("Length")))));
                }
                else
                {
                    var foreachBlockContent = Block(GenerateArgSerializationSyntaxList(definedTypes, arr.ValuesType, varIdExpression));

                    syntaxes.Add(ForEachStatement(IdentifierName("var"), varIdIdentifier, identifierNameSyntax, foreachBlockContent));
                }
            }
            else if (type is IdlBigInt bi)
            {
                bool isUnsigned = bi.TypeName == "u128";


                syntaxes.Add(ExpressionStatement(InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("_data"),
                        IdentifierName("WriteBigInt")),
                    ArgumentList(SeparatedList(new ArgumentSyntax[]
                    {
                            Argument(identifierNameSyntax),
                            Argument(IdentifierName("offset")),
                            Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(16))),
                            Argument(LiteralExpression(isUnsigned ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression))
                    })))));

                syntaxes.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.AddAssignmentExpression, IdentifierName("offset"),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(16)))));
            }
            else if (type is IdlPublicKey)
            {
                syntaxes.Add(ExpressionStatement(InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("_data"),
                        IdentifierName("WritePubKey")),
                    ArgumentList(SeparatedList(new ArgumentSyntax[]
                    {
                            Argument(identifierNameSyntax),
                            Argument(IdentifierName("offset"))
                    })))));

                syntaxes.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.AddAssignmentExpression, IdentifierName("offset"),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(32)))));
            }
            else if (type is IdlOptional optionalType)
            {
                var condition = BinaryExpression(SyntaxKind.NotEqualsExpression, identifierNameSyntax, LiteralExpression(SyntaxKind.NullLiteralExpression));

                List<StatementSyntax> conditionBody = new();

                conditionBody.Add(ExpressionStatement(InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("_data"),
                        IdentifierName("WriteU8")),
                    ArgumentList(SeparatedList(new ArgumentSyntax[]
                    {
                        Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1))),
                        Argument(IdentifierName("offset"))
                    })))));

                conditionBody.Add(ExpressionStatement(AssignmentExpression(
                    SyntaxKind.AddAssignmentExpression,
                    IdentifierName("offset"),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))));

                if (optionalType.ValuesType is IdlValueType 
                    || (optionalType.ValuesType is IdlDefined d 
                        && definedTypes.FirstOrDefault(x => x.Name == d.TypeName) is EnumIdlTypeDefinition e 
                        && e.IsPureEnum()))
                {
                conditionBody.AddRange(GenerateArgSerializationSyntaxList(
                    definedTypes,
                    optionalType.ValuesType,
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        identifierNameSyntax,
                        IdentifierName("Value"))));
                }
                else
                {
                    conditionBody.AddRange(GenerateArgSerializationSyntaxList(
                        definedTypes,
                        optionalType.ValuesType,
                        identifierNameSyntax));
                }
                

                var elseBody = Block(
                    ExpressionStatement(InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("_data"),
                            IdentifierName("WriteU8")),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                            Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))),
                            Argument(IdentifierName("offset"))
                        })))),

                    ExpressionStatement(AssignmentExpression(
                        SyntaxKind.AddAssignmentExpression,
                        IdentifierName("offset"),
                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))));

                syntaxes.Add(IfStatement(
                    condition,
                    Block(conditionBody),
                    ElseClause(elseBody)));
            }
            else
            {
                throw new Exception("Unexpected type " + type.GetType().FullName);
            }

            return syntaxes;
        }

        private (IdentifierNameSyntax, int) GetSerializationValuesForValueType(IdlValueType valueType)
            => valueType.TypeName switch
            {
                "u8" => (IdentifierName("WriteU8"), 1),
                "i8" => (IdentifierName("WriteS8"), 1),
                "u16" => (IdentifierName("WriteU16"), 2),
                "i16" => (IdentifierName("WriteS16"), 2),
                "u32" => (IdentifierName("WriteU32"), 4),
                "i32" => (IdentifierName("WriteS32"), 4),
                "u64" => (IdentifierName("WriteU64"), 8),
                "i64" => (IdentifierName("WriteS64"), 8),
                _ => (IdentifierName("WriteBool"), 1)
            };


        private (IdentifierNameSyntax, int) GetDeserializationValuesForValueType(IdlValueType valueType)
            => valueType.TypeName switch
            {
                "u8" => (IdentifierName("GetU8"), 1),
                "i8" => (IdentifierName("GetS8"), 1),
                "u16" => (IdentifierName("GetU16"), 2),
                "i16" => (IdentifierName("GetS16"), 2),
                "u32" => (IdentifierName("GetU32"), 4),
                "i32" => (IdentifierName("GetS32"), 4),
                "u64" => (IdentifierName("GetU64"), 8),
                "i64" => (IdentifierName("GetS64"), 8),
                _ => (IdentifierName("GetBool"), 1)
            };

        private List<MemberDeclarationSyntax> GenerateAccountsClassSyntaxTree(IIdlAccountItem[] accounts, string v)
        {
            List<MemberDeclarationSyntax> classes = new();
            List<MemberDeclarationSyntax> currentClassMembers = new();

            foreach (var acc in accounts)
            {
                if (acc is IdlAccount singleAcc)
                {
                    currentClassMembers.Add(PropertyDeclaration(
                        List<AttributeListSyntax>(),
                        ClientGeneratorDefaultValues.PublicModifier,
                        IdentifierName("PublicKey"),
                        default,
                        Identifier(singleAcc.Name.ToPascalCase()),
                        ClientGeneratorDefaultValues.PropertyAccessorList));
                }
                else if (acc is IdlAccounts multipleAccounts)
                {
                    classes.AddRange(GenerateAccountsClassSyntaxTree(multipleAccounts.Accounts, v + multipleAccounts.Name.ToPascalCase()));

                    currentClassMembers.Add(PropertyDeclaration(
                        List<AttributeListSyntax>(),
                        ClientGeneratorDefaultValues.PublicModifier,
                        IdentifierName(v + multipleAccounts.Name.ToPascalCase() + "Accounts"),
                        default,
                        Identifier(multipleAccounts.Name.ToPascalCase()),
                        ClientGeneratorDefaultValues.PropertyAccessorList));
                }
            }

            classes.Add(ClassDeclaration(
                List<AttributeListSyntax>(),
                ClientGeneratorDefaultValues.PublicModifier,
                Identifier(v + "Accounts"),
                null,
                null,
                List<TypeParameterConstraintClauseSyntax>(),
                List(currentClassMembers)));

            return classes;
        }


        private MemberDeclarationSyntax GenerateConstructor(Idl idl, string className)
        {
            EqualsValueClauseSyntax programIdArg = null;
            ExpressionSyntax programIdExpression;

            if (idl.DefaultProgramAddress != null)
            {
                programIdExpression = BinaryExpression(
                    SyntaxKind.CoalesceExpression,
                    IdentifierName("programId"),
                    ObjectCreationExpression(
                        IdentifierName("PublicKey"))
                    .WithArgumentList(ArgumentList(SingletonSeparatedList(
                        Argument(LiteralExpression(SyntaxKind.StringLiteralExpression,
                        Literal(idl.DefaultProgramAddress)))))));

                programIdArg = EqualsValueClause(LiteralExpression(SyntaxKind.NullLiteralExpression));
            }
            else
            {
                programIdExpression = IdentifierName("programId");
            }

            var constructorParameters = ParameterList(SeparatedList(new ParameterSyntax[]
            {
                Parameter(List<AttributeListSyntax>(), TokenList(), IdentifierName("IRpcClient"), Identifier("rpcClient"), null),
                Parameter(List<AttributeListSyntax>(), TokenList(), IdentifierName("IStreamingRpcClient"), Identifier("streamingRpcClient"), null),
                Parameter(List<AttributeListSyntax>(), TokenList(), IdentifierName("PublicKey"), Identifier("programId"), programIdArg),
            }));

            var body = Block();

            return ConstructorDeclaration(List<AttributeListSyntax>(), ClientGeneratorDefaultValues.PublicModifier, Identifier(className), constructorParameters, ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, ArgumentList(SeparatedList(new ArgumentSyntax[]
                {
                    Argument(IdentifierName("rpcClient")),
                    Argument(IdentifierName("streamingRpcClient")),
                    Argument(programIdExpression)
                }))), body);
        }


        private MemberDeclarationSyntax GenerateClientSyntaxTree(Idl idl)
        {
            List<MemberDeclarationSyntax> clientMembers = new();

            var className = idl.Name.ToPascalCase() + "Client";

            //clientMembers.AddRange(GenerateFields());
            clientMembers.Add(GenerateConstructor(idl, className));
            //clientMembers.Add(GenerateParseAccount());
            clientMembers.AddRange(GenerateGetAccounts(idl));
            clientMembers.AddRange(GenerateGetAccount(idl));
            clientMembers.AddRange(GenerateSubscribeAccount(idl));

            clientMembers.AddRange(GenerateInstructionBuilderMethods(idl));

            clientMembers.Add(GenerateErrorMapping(idl));

            return ClassDeclaration(className)
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.PartialKeyword)))
                .WithBaseList(
                    BaseList(
                        SingletonSeparatedList<BaseTypeSyntax>(
                            SimpleBaseType(GenericName(Identifier("TransactionalBaseClient"),
                            TypeArgumentList(SingletonSeparatedList<TypeSyntax>(IdentifierName(idl.NamePascalCase + "ErrorKind"))))))))
                .WithMembers(List(clientMembers));
        }

        private MemberDeclarationSyntax GenerateErrorMapping(Idl idl)
        {
            SyntaxNodeOrTokenList syntaxNodeOrTokens = new();

            var enumName = idl.Name.ToPascalCase() + "ErrorKind";


            /// {1u, new ProgramError(EnumKind.EnumVariant, "error message)},
            if (idl.Errors != null)
            {
                foreach (var val in idl.Errors)
                {
                    var errValue = InitializerExpression(
                        SyntaxKind.ComplexElementInitializerExpression,
                        SeparatedList<ExpressionSyntax>(
                            new SyntaxNodeOrToken[]{
                            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(val.Code)),
                            Token(SyntaxKind.CommaToken),
                            ObjectCreationExpression(
                                GenericName(Identifier("ProgramError"))
                                .WithTypeArgumentList(
                                    TypeArgumentList(
                                        SingletonSeparatedList<TypeSyntax>(
                                            IdentifierName(enumName)))),
                                ArgumentList(
                                    SeparatedList(
                                        new []{
                                        Argument(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName(enumName),
                                                IdentifierName(val.Name.ToPascalCase()))),
                                        Argument(
                                            LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                Literal(val.Msg)))})), default)}));
                    syntaxNodeOrTokens = syntaxNodeOrTokens.Add(errValue);
                    syntaxNodeOrTokens = syntaxNodeOrTokens.Add(Token(SyntaxKind.CommaToken));
                }
            }


            var methodDef = MethodDeclaration(
                GenericName(
                    Identifier("Dictionary"))
                .WithTypeArgumentList(
                    TypeArgumentList(
                        SeparatedList<TypeSyntax>(
                            new SyntaxNodeOrToken[]{
                                PredefinedType(Token(SyntaxKind.UIntKeyword)),
                                Token(SyntaxKind.CommaToken),
                                GenericName(Identifier("ProgramError"))
                                .WithTypeArgumentList(
                                    TypeArgumentList(
                                        SingletonSeparatedList<TypeSyntax>(
                                            IdentifierName(enumName))))}))),
                    Identifier("BuildErrorsDictionary"))
            .WithModifiers(TokenList(new[]{
                        Token(SyntaxKind.ProtectedKeyword),
                        Token(SyntaxKind.OverrideKeyword)}))
            .WithBody(Block(SingletonList<StatementSyntax>(
                ReturnStatement(
                    ObjectCreationExpression(
                        GenericName(Identifier("Dictionary"))
                        .WithTypeArgumentList(
                        TypeArgumentList(
                                SeparatedList<TypeSyntax>(
                                    new SyntaxNodeOrToken[]{
                                        PredefinedType(Token(SyntaxKind.UIntKeyword)),
                                        Token(SyntaxKind.CommaToken),
                                        GenericName(Identifier("ProgramError"))
                                        .WithTypeArgumentList(
                                            TypeArgumentList(
                                                SingletonSeparatedList<TypeSyntax>(
                                                    IdentifierName(enumName))))}))))
                    .WithInitializer(
                        InitializerExpression(
                            SyntaxKind.CollectionInitializerExpression,
                            SeparatedList<ExpressionSyntax>(syntaxNodeOrTokens)))))));

            return methodDef;
        }

        private IEnumerable<MemberDeclarationSyntax> GenerateInstructionBuilderMethods(Idl idl)
        {
            List<MemberDeclarationSyntax> methods = new();

            foreach (var instr in idl.Instructions)
            {
                methods.Add(GenerateInstructionBuilder(instr, idl));
            }

            return methods;
        }

        private MemberDeclarationSyntax GenerateInstructionBuilder(IdlInstruction instr, Idl idl)
        {
            List<ParameterSyntax> parameters = new();
            List<ArgumentSyntax> arguments = new();


            EqualsValueClauseSyntax defaultProgram = string.IsNullOrEmpty(idl.DefaultProgramAddress) ? null :
                EqualsValueClause(LiteralExpression(SyntaxKind.NullLiteralExpression));

            parameters.Add(Parameter(List<AttributeListSyntax>(), TokenList(), IdentifierName(instr.Name.ToPascalCase() + "Accounts"), Identifier("accounts"), null));
            arguments.Add(Argument(IdentifierName("accounts")));

            foreach (var arg in instr.Args)
            {
                parameters.Add(Parameter(List<AttributeListSyntax>(), TokenList(), GetTypeSyntax(arg.Type), Identifier(GetNormalizedName(arg.Name)), null));
                arguments.Add(Argument(IdentifierName(GetNormalizedName(arg.Name))));

            }

            parameters.Add(Parameter(List<AttributeListSyntax>(), TokenList(), IdentifierName("PublicKey"), Identifier("feePayer"), null));
            //arguments.Add(Argument(IdentifierName("feePayer")));
            parameters.Add(Parameter(List<AttributeListSyntax>(), TokenList(), GenericName(Identifier("Func"), TypeArgumentList(SeparatedList(new TypeSyntax[] {
                ArrayType(PredefinedType(Token(SyntaxKind.ByteKeyword)), SingletonList(ArrayRankSpecifier(SeparatedList<ExpressionSyntax>()))),
                IdentifierName("PublicKey"),
                ArrayType(PredefinedType(Token(SyntaxKind.ByteKeyword)), SingletonList(ArrayRankSpecifier(SeparatedList<ExpressionSyntax>())))
            }))), Identifier("signingCallback"), null));

            parameters.Add(Parameter(List<AttributeListSyntax>(), TokenList(), IdentifierName("PublicKey"), Identifier("programId"), defaultProgram));
            arguments.Add(Argument(IdentifierName("programId")));


            List<StatementSyntax> body = new();
            if (!string.IsNullOrEmpty(idl.DefaultProgramAddress))
            {
                var pkInit = ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.CoalesceAssignmentExpression,
                    IdentifierName("programId"),
                    ImplicitObjectCreationExpression(
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        Literal(idl.DefaultProgramAddress))))), default)));
                body.Add(pkInit);
            }


            body.Add(LocalDeclarationStatement(VariableDeclaration(QualifiedName(QualifiedName(QualifiedName(IdentifierName("Solnet"), IdentifierName("Rpc")),
                        IdentifierName("Models")),
                IdentifierName("TransactionInstruction")),
                SingletonSeparatedList(VariableDeclarator(
                    Identifier("instr"),
                    null,
                    EqualsValueClause(InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("Program"), IdentifierName(idl.Name.ToPascalCase() + "Program")), IdentifierName(instr.Name.ToPascalCase())),
                        ArgumentList(SeparatedList(arguments)))))))));


            body.Add(ReturnStatement(AwaitExpression(InvocationExpression(IdentifierName("SignAndSendTransaction"), ArgumentList(SeparatedList(new ArgumentSyntax[]
            {
                Argument(IdentifierName("instr")),
                Argument(IdentifierName("feePayer")),
                Argument(IdentifierName("signingCallback"))
            }))))));



            return MethodDeclaration(
                List<AttributeListSyntax>(),
                ClientGeneratorDefaultValues.PublicAwaitModifiers,
                GenericName(Identifier("Task"), TypeArgumentList(SingletonSeparatedList<TypeSyntax>(
                    GenericName(Identifier("RequestResult"), TypeArgumentList(SingletonSeparatedList<TypeSyntax>(PredefinedType(Token(SyntaxKind.StringKeyword)))))))),
                null,
                Identifier("Send" + instr.Name.ToPascalCase() + "Async"),
                null,
                ParameterList(SeparatedList(parameters)),
                List<TypeParameterConstraintClauseSyntax>(),
                Block(body),
                null);
        }

        private SimpleNameSyntax Generic(string generic, string t)
        {
            return GenericName(Identifier(generic), TypeArgumentList(SingletonSeparatedList<TypeSyntax>(IdentifierName(t))));
        }
        private SimpleNameSyntax Generic(string generic, TypeSyntax t)
        {
            return GenericName(Identifier(generic), TypeArgumentList(SingletonSeparatedList(t)));
        }

        private MemberDeclarationSyntax GenerateSubscribeAccount(IIdlTypeDefinitionTy type)
        {
            var callbackBody =
                Block(
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            IdentifierName(type.Name.ToPascalCase()))
                        .WithVariables(
                            SingletonSeparatedList<VariableDeclaratorSyntax>(
                                VariableDeclarator(
                                    Identifier("parsingResult"))
                                .WithInitializer(
                                    EqualsValueClause(
                                        LiteralExpression(
                                            SyntaxKind.NullLiteralExpression)))))),
                    IfStatement(
                        BinaryExpression(
                            SyntaxKind.GreaterThanExpression,
                            ConditionalAccessExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("e"),
                                    IdentifierName("Value")),
                                ConditionalAccessExpression(
                                    MemberBindingExpression(
                                        IdentifierName("Data")),
                                    MemberBindingExpression(
                                        IdentifierName("Count")))),
                            LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                Literal(0))),
                        ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName("parsingResult"),
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(type.Name.ToPascalCase()),
                                        IdentifierName("Deserialize")))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList<ArgumentSyntax>(
                                            Argument(
                                                InvocationExpression(
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("Convert"),
                                                        IdentifierName("FromBase64String")))
                                                .WithArgumentList(
                                                    ArgumentList(
                                                        SingletonSeparatedList<ArgumentSyntax>(
                                                            Argument(
                                                                ElementAccessExpression(
                                                                    MemberAccessExpression(
                                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                                        MemberAccessExpression(
                                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                                            IdentifierName("e"),
                                                                            IdentifierName("Value")),
                                                                        IdentifierName("Data")))
                                                                .WithArgumentList(
                                                                    BracketedArgumentList(
                                                                        SingletonSeparatedList<ArgumentSyntax>(
                                                                            Argument(
                                                                                LiteralExpression(
                                                                                    SyntaxKind.NumericLiteralExpression,
                                                                                    Literal(0))))))))))))))))),
                    ExpressionStatement(
                        InvocationExpression(
                            IdentifierName("callback"))
                        .WithArgumentList(
                            ArgumentList(
                                SeparatedList<ArgumentSyntax>(
                                    new SyntaxNodeOrToken[]{
                                        Argument(
                                            IdentifierName("s")),
                                        Token(SyntaxKind.CommaToken),
                                        Argument(
                                            IdentifierName("e")),
                                        Token(SyntaxKind.CommaToken),
                                        Argument(
                                            IdentifierName("parsingResult"))})))));

            var subscriptionStateDeclaration =
                LocalDeclarationStatement(
                    VariableDeclaration(
                        IdentifierName("SubscriptionState"))
                    .WithVariables(
                        SingletonSeparatedList<VariableDeclaratorSyntax>(
                            VariableDeclarator(
                                Identifier("res"))
                            .WithInitializer(
                                EqualsValueClause(
                                    AwaitExpression(
                                        InvocationExpression(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName("StreamingRpcClient"),
                                                IdentifierName("SubscribeAccountInfoAsync")))
                                        .WithArgumentList(
                                            ArgumentList(
                                                SeparatedList<ArgumentSyntax>(
                                                    new SyntaxNodeOrToken[]{
                                                        Argument(
                                                            IdentifierName("accountAddress")),
                                                        Token(SyntaxKind.CommaToken),
                                                        Argument(
                                                            ParenthesizedLambdaExpression()
                                                            .WithParameterList(
                                                                ParameterList(
                                                                    SeparatedList<ParameterSyntax>(
                                                                        new SyntaxNodeOrToken[]{
                                                                            Parameter(
                                                                                Identifier("s")),
                                                                            Token(SyntaxKind.CommaToken),
                                                                            Parameter(
                                                                                Identifier("e"))})))
                                                            .WithBlock(callbackBody)),
                                                        Token(SyntaxKind.CommaToken),
                                                        Argument(
                                                            IdentifierName("commitment"))})))))))));
            

            var methodDeclaration = MethodDeclaration(
                        GenericName(
                            Identifier("Task"))
                        .WithTypeArgumentList(
                            TypeArgumentList(
                                SingletonSeparatedList<TypeSyntax>(
                                    IdentifierName("SubscriptionState")))),
                        Identifier("Subscribe" + type.Name.ToPascalCase() + "Async"))
                    .WithModifiers(
                        TokenList(
                            new []{
                                Token(SyntaxKind.PublicKeyword),
                                Token(SyntaxKind.AsyncKeyword)}))
                    .WithParameterList(
                        ParameterList(
                            SeparatedList<ParameterSyntax>(
                                new SyntaxNodeOrToken[]{
                                    Parameter(
                                        Identifier("accountAddress"))
                                    .WithType(
                                        PredefinedType(
                                            Token(SyntaxKind.StringKeyword))),
                                    Token(SyntaxKind.CommaToken),
                                    Parameter(
                                        Identifier("callback"))
                                    .WithType(
                                        GenericName(
                                            Identifier("Action"))
                                        .WithTypeArgumentList(
                                            TypeArgumentList(
                                                SeparatedList<TypeSyntax>(
                                                    new SyntaxNodeOrToken[]{
                                                        IdentifierName("SubscriptionState"),
                                                        Token(SyntaxKind.CommaToken),
                                                        QualifiedName(
                                                            QualifiedName(
                                                                QualifiedName(
                                                                    IdentifierName("Solnet"),
                                                                    IdentifierName("Rpc")),
                                                                IdentifierName("Messages")),
                                                            GenericName(
                                                                Identifier("ResponseValue"))
                                                            .WithTypeArgumentList(
                                                                TypeArgumentList(
                                                                    SingletonSeparatedList<TypeSyntax>(
                                                                        QualifiedName(
                                                                            QualifiedName(
                                                                                QualifiedName(
                                                                                    IdentifierName("Solnet"),
                                                                                    IdentifierName("Rpc")),
                                                                                IdentifierName("Models")),
                                                                            IdentifierName("AccountInfo")))))),
                                                        Token(SyntaxKind.CommaToken),
                                                        IdentifierName(type.Name.ToPascalCase())})))),
                                    Token(SyntaxKind.CommaToken),
                                    Parameter(
                                        Identifier("commitment"))
                                    .WithType(
                                        IdentifierName("Commitment"))
                                    .WithDefault(
                                        EqualsValueClause(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName("Commitment"),
                                                IdentifierName("Finalized"))))})));

            return methodDeclaration
                    .WithBody(
                        Block(
                            subscriptionStateDeclaration,
                            ReturnStatement(
                                IdentifierName("res")))
                    );
        }

        private MemberDeclarationSyntax GenerateGetAccount(IIdlTypeDefinitionTy type)
        {
            List<StatementSyntax> body = new();

            var typeName = type.Name.ToPascalCase();

            body.Add(LocalDeclarationStatement(VariableDeclaration(IdentifierName("var"),
                SingletonSeparatedList(VariableDeclarator(Identifier("res"), null, EqualsValueClause(
                    AwaitExpression(InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("RpcClient"),
                            IdentifierName("GetAccountInfoAsync")),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                            Argument(IdentifierName("accountAddress")),
                            Argument(IdentifierName("commitment"))
                        }
                        ))))))))));

            var condition =
                    PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("res"), IdentifierName("WasSuccessful")));


            var ifBody = ReturnStatement(ObjectCreationExpression(QualifiedName(QualifiedName(QualifiedName(IdentifierName("Solnet"), IdentifierName("Programs")), IdentifierName("Models")),
                       Generic("AccountResultWrapper", typeName)),
                       ArgumentList(SingletonSeparatedList<ArgumentSyntax>(Argument(IdentifierName("res")))), default));

            body.Add(IfStatement(condition, ifBody));

            var convertArg = ElementAccessExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("res"), IdentifierName("Result")), IdentifierName("Value")), IdentifierName("Data")),
                                BracketedArgumentList(SingletonSeparatedList(Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))))));

            var convert = InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("Convert"), IdentifierName("FromBase64String")),
                            ArgumentList(SingletonSeparatedList(Argument(convertArg))));

            var desser = InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(typeName), IdentifierName("Deserialize")),
                            ArgumentList(SingletonSeparatedList(Argument(convert))));

            body.Add(LocalDeclarationStatement(VariableDeclaration(IdentifierName("var"),
                SingletonSeparatedList(VariableDeclarator(Identifier("resultingAccount"), null, EqualsValueClause(desser))))));

            var retVal = ObjectCreationExpression(QualifiedName(QualifiedName(QualifiedName(IdentifierName("Solnet"), IdentifierName("Programs")), IdentifierName("Models")),
                       Generic("AccountResultWrapper", typeName)), ArgumentList(SeparatedList(new[]
                       {
                           Argument(IdentifierName("res")),
                           Argument(IdentifierName("resultingAccount"))
                       })), default);

            body.Add(ReturnStatement(retVal));

            // Solnet.Rpc.Types.Commitment commitment = Commitment.Finalized
            EqualsValueClauseSyntax defaultCommitment = EqualsValueClause(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("Commitment"), IdentifierName("Finalized")));

            return MethodDeclaration(List<AttributeListSyntax>(),
                       ClientGeneratorDefaultValues.PublicAwaitModifiers,

                       Generic("Task", QualifiedName(QualifiedName(QualifiedName(IdentifierName("Solnet"), IdentifierName("Programs")), IdentifierName("Models")),
                       Generic("AccountResultWrapper", typeName)))

,
                       null,
                       Identifier("Get" + typeName + "Async"),
                       null,
                       ParameterList(SeparatedList(new ParameterSyntax[] {
                        Parameter(List<AttributeListSyntax>(), TokenList(), PredefinedType(Token(SyntaxKind.StringKeyword)), Identifier("accountAddress"), default),

                        Parameter(List<AttributeListSyntax>(), TokenList(), IdentifierName("Commitment"), Identifier("commitment"), defaultCommitment)

                       })),
                       List<TypeParameterConstraintClauseSyntax>(),
                       Block(body),
                       null);
        }
        private MemberDeclarationSyntax GenerateGetAccounts(Idl idl, IIdlTypeDefinitionTy type)
        {
            List<StatementSyntax> body = new();

            var typeName = type.Name.ToPascalCase();

            //build memcmp filters

            var memCmpType = QualifiedName(QualifiedName(QualifiedName(IdentifierName("Solnet"), IdentifierName("Rpc")), IdentifierName("Models")), IdentifierName("MemCmp"));
            var memCmp = ObjectCreationExpression(memCmpType, default,
                    InitializerExpression(SyntaxKind.ObjectInitializerExpression, SeparatedList<ExpressionSyntax>(new[]{
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName("Bytes"), QualifiedName(IdentifierName(typeName), IdentifierName("ACCOUNT_DISCRIMINATOR_B58"))),
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName("Offset"), LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))) })));


            body.Add(LocalDeclarationStatement(VariableDeclaration(IdentifierName("var"),
                SingletonSeparatedList(VariableDeclarator(Identifier("list"), null, EqualsValueClause(
                    ObjectCreationExpression(Generic("List", memCmpType),
                    default,
                    InitializerExpression(SyntaxKind.CollectionInitializerExpression, SingletonSeparatedList<ExpressionSyntax>(memCmp))
                    )))))));

            body.Add(LocalDeclarationStatement(VariableDeclaration(IdentifierName("var"),
                SingletonSeparatedList(VariableDeclarator(Identifier("res"), null, EqualsValueClause(
                    AwaitExpression(InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("RpcClient"),
                            IdentifierName("GetProgramAccountsAsync")),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                            Argument(IdentifierName("programAddress")),
                            Argument(IdentifierName("commitment")),
                            Argument(NameColon(IdentifierName("memCmpList")), default, IdentifierName("list"))
                        }
                        ))))))))));

            /**
             * 
             * 
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new ProgramAccountsResultWrapper<List<T>>(res);

             */

            var condition = BinaryExpression(SyntaxKind.LogicalOrExpression,
                    PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("res"), IdentifierName("WasSuccessful"))),
                    PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, ParenthesizedExpression(BinaryExpression(SyntaxKind.GreaterThanExpression,
                        ConditionalAccessExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("res"), IdentifierName("Result")),
                            MemberBindingExpression(IdentifierName("Count"))),
                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))))));

            //new Solnet.Programs.Models.ProgramAccountsResultWrapper<List<Reserve>>(res);

            var ifBody = ReturnStatement(ObjectCreationExpression(QualifiedName(QualifiedName(QualifiedName(IdentifierName("Solnet"), IdentifierName("Programs")), IdentifierName("Models")),
                       Generic("ProgramAccountsResultWrapper", Generic("List", typeName))),
                       ArgumentList(SingletonSeparatedList<ArgumentSyntax>(Argument(IdentifierName("res")))), default));

            body.Add(IfStatement(condition, ifBody));

            /*
            List<T> resultingAccounts = new(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result =>
                DeserializeAccount<T>(Convert.FromBase64String(result.Account.Data[0]))));

            return new ProgramAccountsResultWrapper<List<T>>(res, resultingAccounts);
             * 
             */

            var listInitialCapacity = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("res"), IdentifierName("Result")),
                                IdentifierName("Count"));

            var listType = Generic("List", typeName);

            body.Add(LocalDeclarationStatement(VariableDeclaration(listType,
                SingletonSeparatedList(VariableDeclarator(Identifier("resultingAccounts"), default,
                        EqualsValueClause(ObjectCreationExpression(listType,
                            ArgumentList(SingletonSeparatedList(Argument(listInitialCapacity))),
                            default)))))));

            var convertArg = ElementAccessExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("result"), IdentifierName("Account")), IdentifierName("Data")),
                                BracketedArgumentList(SingletonSeparatedList(Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))))));

            var convert = InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("Convert"), IdentifierName("FromBase64String")),
                            ArgumentList(SingletonSeparatedList(Argument(convertArg))));

            var desser = InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(typeName), IdentifierName("Deserialize")),
                            ArgumentList(SingletonSeparatedList(Argument(convert))));

            var lambda = SimpleLambdaExpression(Parameter(Identifier("result")), desser);

            var selectCall = InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("res"), IdentifierName("Result")), IdentifierName("Select")),
                            ArgumentList(SingletonSeparatedList(Argument(lambda))));



            body.Add(ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("resultingAccounts"), IdentifierName("AddRange")),
                            ArgumentList(SingletonSeparatedList(Argument(selectCall))))));

            var retVal = ObjectCreationExpression(QualifiedName(QualifiedName(QualifiedName(IdentifierName("Solnet"), IdentifierName("Programs")), IdentifierName("Models")),
                       Generic("ProgramAccountsResultWrapper", Generic("List", typeName))), ArgumentList(SeparatedList(new[]
                       {
                           Argument(IdentifierName("res")),
                           Argument(IdentifierName("resultingAccounts"))
                       })), default);

            body.Add(ReturnStatement(retVal));

            // Solnet.Rpc.Types.Commitment commitment = Commitment.Finalized

            EqualsValueClauseSyntax defaultProgram = string.IsNullOrEmpty(idl.DefaultProgramAddress) ? null : EqualsValueClause(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(idl.DefaultProgramAddress)));
            EqualsValueClauseSyntax defaultCommitment = EqualsValueClause(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("Commitment"), IdentifierName("Finalized")));

            return MethodDeclaration(List<AttributeListSyntax>(),
                       ClientGeneratorDefaultValues.PublicAwaitModifiers,

                       Generic("Task", QualifiedName(QualifiedName(QualifiedName(IdentifierName("Solnet"), IdentifierName("Programs")), IdentifierName("Models")),
                       Generic("ProgramAccountsResultWrapper", Generic("List", typeName))))

,
                       null,
                       Identifier("Get" + typeName + "sAsync"),
                       null,
                       ParameterList(SeparatedList(new ParameterSyntax[] {
                        Parameter(List<AttributeListSyntax>(), TokenList(), PredefinedType(Token(SyntaxKind.StringKeyword)), Identifier("programAddress"), defaultProgram),

                        Parameter(List<AttributeListSyntax>(), TokenList(), IdentifierName("Commitment"), Identifier("commitment"), defaultCommitment)

                       })),
                       List<TypeParameterConstraintClauseSyntax>(),
                       Block(body),
                       null);
        }

        private List<MemberDeclarationSyntax> GenerateGetAccounts(Idl idl)
        {
            List<MemberDeclarationSyntax> methods = new();

            foreach (var acc in idl.Accounts)
            {
                methods.Add(GenerateGetAccounts(idl, acc));
            }


            return methods;
        }
        
        private List<MemberDeclarationSyntax> GenerateSubscribeAccount(Idl idl)
        {
            List<MemberDeclarationSyntax> methods = new();

            foreach (var acc in idl.Accounts)
            {
                methods.Add(GenerateSubscribeAccount(acc));
            }


            return methods;
        }

        private List<MemberDeclarationSyntax> GenerateGetAccount(Idl idl)
        {
            List<MemberDeclarationSyntax> methods = new();

            foreach (var acc in idl.Accounts)
            {
                methods.Add(GenerateGetAccount(acc));
            }


            return methods;
        }

        private MemberDeclarationSyntax GenerateTypesSyntaxTree(Idl idl)
        {
            List<MemberDeclarationSyntax> types = new();

            for (int i = 0; i < idl.Types.Length; i++)
            {
                if (IsTypeReferenced(idl, idl.Types[i]))
                    types.AddRange(GenerateTypeDeclaration(idl, idl.Types[i], true));
            }

            return NamespaceDeclaration(IdentifierName("Types"), List<ExternAliasDirectiveSyntax>(), List<UsingDirectiveSyntax>(), List(types));
        }

        private bool IsTypeReferenced(Idl idl, IIdlTypeDefinitionTy idlTypeDefinitionTy)
        {
            string typeName = idlTypeDefinitionTy.Name;

            foreach (var type in idl.Types)
            {
                if (IsTypeReferenced(type, typeName)) return true;
            }

            foreach (var type in idl.Accounts)
            {
                if (IsTypeReferenced(type, typeName)) return true;
            }

            foreach (var instr in idl.Instructions)
            {
                foreach (var arg in instr.Args)
                    if (IsTypeReferenced(arg.Type, typeName)) return true;
            }

            return false;
        }

        private bool IsTypeReferenced(IIdlTypeDefinitionTy typeToCheck, string typeName)
        => typeToCheck switch
        {
            StructIdlTypeDefinition structIdl => IsTypeReferenced(structIdl, typeName),
            EnumIdlTypeDefinition enumIdl => IsTypeReferenced(enumIdl, typeName),
            _ => false
        };

        private bool IsTypeReferenced(StructIdlTypeDefinition type, string typeName)
        => type.Fields.Any(f => IsTypeReferenced(f.Type, typeName));

        private bool IsTypeReferenced(EnumIdlTypeDefinition type, string typeName)
        => type.Variants.Any(v => v switch
        {
            NamedFieldsEnumVariant named => IsTypeReferenced(named, typeName),
            TupleFieldsEnumVariant tuple => IsTypeReferenced(tuple, typeName),
            _ => false
        });

        private bool IsTypeReferenced(NamedFieldsEnumVariant variant, string typeName)
        => variant.Fields.Any(f => IsTypeReferenced(f.Type, typeName));
        private bool IsTypeReferenced(TupleFieldsEnumVariant variant, string typeName)
        => variant.Fields.Any(f => IsTypeReferenced(f, typeName));

        private bool IsTypeReferenced(IIdlType type, string typeName)
        => type switch
        {
            IdlDefined t => t.TypeName == typeName,
            IdlArray arr => IsTypeReferenced(arr.ValuesType, typeName),
            IdlOptional opt => IsTypeReferenced(opt.ValuesType, typeName),
            _ => false
        };


        private SyntaxList<MemberDeclarationSyntax> GenerateTypeDeclaration(Idl idl, IIdlTypeDefinitionTy idlTypeDefinitionTy, bool generateSerialization, bool isAccount = false)
            => idlTypeDefinitionTy switch
            {
                StructIdlTypeDefinition structIdl => GenerateClassDeclaration(idl, structIdl, generateSerialization, isAccount),
                EnumIdlTypeDefinition enumIdl => GenerateEnumDeclaration(idl, enumIdl, generateSerialization),
                _ => throw new Exception("bad type")
            };

        private TypeSyntax GetTypeSyntax(IIdlType type)
            => type switch
            {
                IdlArray arr => ArrayType(GetTypeSyntax(arr.ValuesType), SingletonList(ArrayRankSpecifier())),
                IdlBigInt => IdentifierName("BigInteger"),
                IdlDefined def => IdentifierName(def.TypeName),
                IdlOptional opt => opt.ValuesType switch
                {
                    IdlValueType => NullableType(GetTypeSyntax(opt.ValuesType)),
                    _ => GetTypeSyntax(opt.ValuesType)
                },
                IdlPublicKey => IdentifierName("PublicKey"),
                IdlString => PredefinedType(Token(SyntaxKind.StringKeyword)),
                IdlValueType v => PredefinedType(Token(GetTokenForValueType(v))),
                _ => throw new Exception("huh wat")
            };

        private SyntaxKind GetTokenForValueType(IdlValueType idlValueType)
            => idlValueType.TypeName switch
            {
                "u8" => SyntaxKind.ByteKeyword,
                "i8" => SyntaxKind.SByteKeyword,
                "u16" => SyntaxKind.UShortKeyword,
                "i16" => SyntaxKind.ShortKeyword,
                "u32" => SyntaxKind.UIntKeyword,
                "i32" => SyntaxKind.IntKeyword,
                "u64" => SyntaxKind.ULongKeyword,
                "i64" => SyntaxKind.LongKeyword,
                _ => SyntaxKind.BoolKeyword
            };

        private List<MemberDeclarationSyntax> GenerateAccountDiscriminator(StructIdlTypeDefinition structIdl)
        {
            var members = new List<MemberDeclarationSyntax>();

            members.Add(PropertyDeclaration(List<AttributeListSyntax>(), ClientGeneratorDefaultValues.PublicStaticModifiers,
                PredefinedType(Token(SyntaxKind.ULongKeyword)), default, Identifier("ACCOUNT_DISCRIMINATOR"), default,
                ArrowExpressionClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(SigHash.GetAccountSignatureHash(structIdl.Name)))), default, Token(SyntaxKind.SemicolonToken)));

            var sigHash = SigHash.GetAccountSignatureHash(structIdl.Name);
            var buffer = new byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, sigHash);

            List<ExpressionSyntax> init = new();

            var b58 = Encoders.Base58.EncodeData(buffer);

            foreach (var b in buffer)
                init.Add(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(b)));

            members.Add(PropertyDeclaration(List<AttributeListSyntax>(), ClientGeneratorDefaultValues.PublicStaticModifiers,
                Generic("ReadOnlySpan", PredefinedType(Token(SyntaxKind.ByteKeyword))), default, Identifier("ACCOUNT_DISCRIMINATOR_BYTES"), default,
                ArrowExpressionClause(
                    ArrayCreationExpression(ArrayType(PredefinedType(Token(SyntaxKind.ByteKeyword)), SingletonList(ArrayRankSpecifier())), InitializerExpression(SyntaxKind.ArrayInitializerExpression, SeparatedList(init)))),
                default, Token(SyntaxKind.SemicolonToken)));

            members.Add(PropertyDeclaration(List<AttributeListSyntax>(), ClientGeneratorDefaultValues.PublicStaticModifiers,
                PredefinedType(Token(SyntaxKind.StringKeyword)), default, Identifier("ACCOUNT_DISCRIMINATOR_B58"), default,
                ArrowExpressionClause(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(b58))), default, Token(SyntaxKind.SemicolonToken)));

            return members;
        }

        private SyntaxList<MemberDeclarationSyntax> GenerateClassDeclaration(Idl idl, StructIdlTypeDefinition structIdl, bool generateSerialization, bool isAccount = false)
        {
            List<MemberDeclarationSyntax> classMembers = new();

            if (isAccount)
                classMembers.AddRange(GenerateAccountDiscriminator(structIdl));

            foreach (var field in structIdl.Fields)
            {
                classMembers.Add(PropertyDeclaration(List<AttributeListSyntax>(), ClientGeneratorDefaultValues.PublicModifier, GetTypeSyntax(field.Type), default, Identifier(field.Name.ToPascalCase()), ClientGeneratorDefaultValues.PropertyAccessorList));
            }

            if (generateSerialization)
            {
                List<StatementSyntax> serializationBody = new();
                serializationBody.Add(LocalDeclarationStatement(VariableDeclaration(PredefinedType(Token(SyntaxKind.IntKeyword)), SingletonSeparatedList(VariableDeclarator(Identifier("offset"), null, EqualsValueClause(IdentifierName("initialOffset")))))));


                foreach (var field in structIdl.Fields)
                {
                    serializationBody.AddRange(GenerateArgSerializationSyntaxList(idl.Types, field.Type, IdentifierName(field.Name.ToPascalCase())));
                }

                serializationBody.Add(ReturnStatement(BinaryExpression(SyntaxKind.SubtractExpression, IdentifierName("offset"), IdentifierName("initialOffset"))));


                classMembers.Add(MethodDeclaration(List<AttributeListSyntax>(),
                    ClientGeneratorDefaultValues.PublicModifier,
                    PredefinedType(Token(SyntaxKind.IntKeyword)),
                    null,
                    Identifier("Serialize"),
                    null,
                    ParameterList(SeparatedList(new ParameterSyntax[] {
                        Parameter(List<AttributeListSyntax>(), TokenList(), ArrayType(PredefinedType(Token(SyntaxKind.ByteKeyword)), SingletonList(ArrayRankSpecifier())), Identifier("_data"), null),
                        Parameter(List<AttributeListSyntax>(), TokenList(), PredefinedType(Token(SyntaxKind.IntKeyword)), Identifier("initialOffset"), null),

                    })),
                    List<TypeParameterConstraintClauseSyntax>(),
                    Block(serializationBody),
                    null));
            }

            List<StatementSyntax> desserializationBody = new();

            if (isAccount)
            {


                desserializationBody.Add(LocalDeclarationStatement(VariableDeclaration(PredefinedType(Token(SyntaxKind.IntKeyword)),
                    SingletonSeparatedList(VariableDeclarator(Identifier("offset"), null,
                    EqualsValueClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))))))));

                // skip first 8 bytes
                desserializationBody.Add(LocalDeclarationStatement(VariableDeclaration(PredefinedType(Token(SyntaxKind.ULongKeyword)),
                    SingletonSeparatedList(VariableDeclarator(Identifier("accountHashValue"), null,
                    EqualsValueClause(InvocationExpression(MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("_data"),
                        IdentifierName("GetU64")),
                    ArgumentList(SeparatedList(new ArgumentSyntax[]
                    {
                        Argument(IdentifierName("offset"))
                    })))))))));

                desserializationBody.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.AddAssignmentExpression, IdentifierName("offset"),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(8)))));

                var condition = BinaryExpression(
                    SyntaxKind.NotEqualsExpression,
                    IdentifierName("accountHashValue"),
                    IdentifierName("ACCOUNT_DISCRIMINATOR"));

                var body = ReturnStatement(LiteralExpression(SyntaxKind.NullLiteralExpression));

                desserializationBody.Add(IfStatement(condition, Block(body), null));
            }
            else
            {

                desserializationBody.Add(LocalDeclarationStatement(VariableDeclaration(PredefinedType(Token(SyntaxKind.IntKeyword)),
                    SingletonSeparatedList(VariableDeclarator(Identifier("offset"), null,
                    EqualsValueClause(IdentifierName("initialOffset")))))));
            }

            var resultVariableToken = Identifier("result");
            var resulVariable = IdentifierName(resultVariableToken);
            var constructorCallExpression = ObjectCreationExpression(IdentifierName(structIdl.Name.ToPascalCase()), ArgumentList(), null);


            if (isAccount)
            {
                desserializationBody.Add(LocalDeclarationStatement(VariableDeclaration(IdentifierName(structIdl.Name.ToPascalCase()),
                SingletonSeparatedList(VariableDeclarator(resultVariableToken, null,
                EqualsValueClause(constructorCallExpression))))));
            }
            else
            {
                desserializationBody.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, resulVariable, constructorCallExpression)));
            }

            foreach (var field in structIdl.Fields)
            {
                desserializationBody.AddRange(GenerateDeserializationSyntaxList(idl.Types, field.Type, MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, resulVariable, IdentifierName(field.Name.ToPascalCase()))));
            }

            // if account -> public static AccName Deserialize(byte[] data)
            // else ->       public static int Deserialize(byte[] data, int initialOffset, out ObjName result)

            if (isAccount)
            {
                desserializationBody.Add(ReturnStatement(resulVariable));

                classMembers.Add(MethodDeclaration(List<AttributeListSyntax>(),
                    ClientGeneratorDefaultValues.PublicStaticModifiers,
                    IdentifierName(structIdl.Name.ToPascalCase()),
                    null,
                    Identifier("Deserialize"),
                    null,
                    ParameterList(SeparatedList(new ParameterSyntax[] {
                        Parameter(List<AttributeListSyntax>(), TokenList(), GenericName(Identifier("ReadOnlySpan"), TypeArgumentList(SingletonSeparatedList<TypeSyntax>(PredefinedType(Token(SyntaxKind.ByteKeyword))))), Identifier("_data"), null)
                    })),
                    List<TypeParameterConstraintClauseSyntax>(),
                    Block(desserializationBody),
                    null));
            }
            else
            {
                desserializationBody.Add(ReturnStatement(BinaryExpression(SyntaxKind.SubtractExpression, IdentifierName("offset"), IdentifierName("initialOffset"))));
                //IdentifierName(structIdl.Name.ToPascalCase()),
                classMembers.Add(MethodDeclaration(List<AttributeListSyntax>(),
                    ClientGeneratorDefaultValues.PublicStaticModifiers,
                    PredefinedType(Token(SyntaxKind.IntKeyword)),
                    null,
                    Identifier("Deserialize"),
                    null,
                    ParameterList(SeparatedList(new ParameterSyntax[] {
                        Parameter(List<AttributeListSyntax>(), TokenList(), GenericName(Identifier("ReadOnlySpan"), TypeArgumentList(SingletonSeparatedList<TypeSyntax>(PredefinedType(Token(SyntaxKind.ByteKeyword))))), Identifier("_data"), null),
                        Parameter(List<AttributeListSyntax>(), TokenList(), PredefinedType(Token(SyntaxKind.IntKeyword)), Identifier("initialOffset"), null),
                        Parameter(List<AttributeListSyntax>(), TokenList(Token(SyntaxKind.OutKeyword)), IdentifierName(structIdl.Name.ToPascalCase()), resultVariableToken, null)
                    })),
                    List<TypeParameterConstraintClauseSyntax>(),
                    Block(desserializationBody),
                    null));
            }

            return SingletonList<MemberDeclarationSyntax>(ClassDeclaration(List<AttributeListSyntax>(), ClientGeneratorDefaultValues.PublicPartialModifiers, Identifier(structIdl.Name.ToPascalCase()), null, null, List<TypeParameterConstraintClauseSyntax>(), List(classMembers)));
        }

        private SyntaxList<MemberDeclarationSyntax> GenerateEnumDeclaration(Idl idl, EnumIdlTypeDefinition enumIdl, bool generateSerialization)
        {
            List<EnumMemberDeclarationSyntax> enumMembers = new();
            List<MemberDeclarationSyntax> supportClasses = new();
            List<MemberDeclarationSyntax> mainClassProperties = new();

            List<SwitchSectionSyntax> serializationCases = new();
            List<SwitchSectionSyntax> deSerializationCases = new();

            List<StatementSyntax> desserializationBody = new();


            //switchstatement
            //  switchsection
            //    switchlabel - expression
            //    block - body (stmt, breakstmt)
            //

            foreach (var member in enumIdl.Variants)
            {
                enumMembers.Add(EnumMemberDeclaration(member.Name));

                List<StatementSyntax> caseStatements = new();
                List<StatementSyntax> caseStatementsDeser = new();

                if (member is TupleFieldsEnumVariant tuple)
                {
                    List<TypeSyntax> typeSyntaxes = new();
                    List<StatementSyntax> tupleSerializationStatements = new();
                    List<ArgumentSyntax> tupleConstructionArgs = new();

                    for (int i = 0; i < tuple.Fields.Length; i++)
                    {
                        typeSyntaxes.Add(GetTypeSyntax(tuple.Fields[i]));
                        caseStatements.AddRange(GenerateArgSerializationSyntaxList(
                            idl.Types,
                            tuple.Fields[i],
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(member.Name.ToPascalCase() + "Value"), IdentifierName("Item" + (i + 1)))));


                        caseStatementsDeser.Add(LocalDeclarationStatement(VariableDeclaration(GetTypeSyntax(tuple.Fields[i]), SingletonSeparatedList(VariableDeclarator(member.Name + "Item" + (i + 1))))));

                        caseStatementsDeser.AddRange(GenerateDeserializationSyntaxList(idl.Types, tuple.Fields[i],
                            IdentifierName(member.Name + "Item" + (i + 1))));

                        tupleConstructionArgs.Add(Argument(IdentifierName(member.Name + "Item" + (i + 1))));
                    }

                    caseStatementsDeser.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("result"), IdentifierName(member.Name.ToPascalCase() + "Value")),
                        InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("Tuple"), IdentifierName("Create")), ArgumentList(SeparatedList(tupleConstructionArgs))))));

                    mainClassProperties.Add(PropertyDeclaration(List<AttributeListSyntax>(), ClientGeneratorDefaultValues.PublicModifier, GenericName(Identifier("Tuple"), TypeArgumentList(SeparatedList(typeSyntaxes))), default, Identifier(member.Name.ToPascalCase() + "Value"), ClientGeneratorDefaultValues.PropertyAccessorList));

                }
                else if (member is NamedFieldsEnumVariant structVariant)
                {
                    List<MemberDeclarationSyntax> fields = new();
                    List<StatementSyntax> body = new();
                    List<StatementSyntax> deserBody = new();


                    body.Add(LocalDeclarationStatement(VariableDeclaration(PredefinedType(Token(SyntaxKind.IntKeyword)),
                            SingletonSeparatedList(VariableDeclarator(Identifier("offset"), null,
                            EqualsValueClause(IdentifierName("initialOffset")))))));
                    deserBody.Add(LocalDeclarationStatement(VariableDeclaration(PredefinedType(Token(SyntaxKind.IntKeyword)),
                            SingletonSeparatedList(VariableDeclarator(Identifier("offset"), null,
                            EqualsValueClause(IdentifierName("initialOffset")))))));

                    deserBody.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName("result"), ObjectCreationExpression(IdentifierName(structVariant.Name.ToPascalCase() + "Type"), ArgumentList(), null))));


                    foreach (var field in structVariant.Fields)
                    {
                        fields.Add(PropertyDeclaration(List<AttributeListSyntax>(), ClientGeneratorDefaultValues.PublicModifier, GetTypeSyntax(field.Type), default, Identifier(field.Name.ToPascalCase()), ClientGeneratorDefaultValues.PropertyAccessorList));
                    }

                    foreach (var field in structVariant.Fields)
                    {
                        body.AddRange(GenerateArgSerializationSyntaxList(idl.Types, field.Type, IdentifierName(field.Name.ToPascalCase())));
                    }

                    foreach (var field in structVariant.Fields)
                    {
                        deserBody.AddRange(GenerateDeserializationSyntaxList(idl.Types, field.Type, MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("result"), IdentifierName(field.Name.ToPascalCase()))));
                    }

                    body.Add(ReturnStatement(BinaryExpression(SyntaxKind.SubtractExpression, IdentifierName("offset"), IdentifierName("initialOffset"))));


                    caseStatementsDeser.Add(LocalDeclarationStatement(VariableDeclaration(IdentifierName(structVariant.Name.ToPascalCase() + "Type"), SingletonSeparatedList(VariableDeclarator(Identifier("tmp" + structVariant.Name.ToPascalCase() + "Value"), null, EqualsValueClause(ObjectCreationExpression(IdentifierName(structVariant.Name.ToPascalCase() + "Type"), ArgumentList(), null)))))));


                    caseStatementsDeser.Add(ExpressionStatement(AssignmentExpression(
                        SyntaxKind.AddAssignmentExpression,
                        IdentifierName("offset"),
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(structVariant.Name.ToPascalCase() + "Type"),
                                IdentifierName("Deserialize")),
                            ArgumentList(SeparatedList(new ArgumentSyntax[]
                            {
                                Argument(IdentifierName("_data")),
                                Argument(IdentifierName("offset")),
                                Argument(null, Token(SyntaxKind.OutKeyword),IdentifierName("tmp" + structVariant.Name.ToPascalCase() + "Value"))

                            }))))));

                    caseStatementsDeser.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("result"), IdentifierName(structVariant.Name.ToPascalCase() + "Value")), IdentifierName("tmp" + structVariant.Name.ToPascalCase() + "Value"))));


                    deserBody.Add(ReturnStatement(BinaryExpression(SyntaxKind.SubtractExpression, IdentifierName("offset"), IdentifierName("initialOffset"))));
                    //IdentifierName(structIdl.Name.ToPascalCase()),
                    fields.Add(MethodDeclaration(List<AttributeListSyntax>(),
                            ClientGeneratorDefaultValues.PublicStaticModifiers,
                            PredefinedType(Token(SyntaxKind.IntKeyword)),
                            null,
                            Identifier("Deserialize"),
                            null,
                            ParameterList(SeparatedList(new ParameterSyntax[] {
                        Parameter(List<AttributeListSyntax>(), TokenList(), GenericName(Identifier("ReadOnlySpan"), TypeArgumentList(SingletonSeparatedList<TypeSyntax>(PredefinedType(Token(SyntaxKind.ByteKeyword))))), Identifier("_data"), null),
                        Parameter(List<AttributeListSyntax>(), TokenList(), PredefinedType(Token(SyntaxKind.IntKeyword)), Identifier("initialOffset"), null),
                        Parameter(List<AttributeListSyntax>(), TokenList(Token(SyntaxKind.OutKeyword)), IdentifierName(structVariant.Name.ToPascalCase() + "Type"), Identifier("result"), null)
                            })),
                            List<TypeParameterConstraintClauseSyntax>(),
                            Block(deserBody),
                            null));

                    fields.Add(MethodDeclaration(List<AttributeListSyntax>(),
                        ClientGeneratorDefaultValues.PublicModifier,
                        PredefinedType(Token(SyntaxKind.IntKeyword)),
                        null,
                        Identifier("Serialize"),
                        null,
                        ParameterList(SeparatedList(new ParameterSyntax[] {
                            Parameter(List<AttributeListSyntax>(), TokenList(), ArrayType(PredefinedType(Token(SyntaxKind.ByteKeyword)), SingletonList(ArrayRankSpecifier())), Identifier("_data"), null),
                            Parameter(List<AttributeListSyntax>(), TokenList(), PredefinedType(Token(SyntaxKind.IntKeyword)), Identifier("initialOffset"), null),
                        })),
                        List<TypeParameterConstraintClauseSyntax>(),
                        Block(body),
                        null));

                    caseStatements.Add(ExpressionStatement(AssignmentExpression(
                        SyntaxKind.AddAssignmentExpression,
                        IdentifierName("offset"), InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(member.Name.ToPascalCase() + "Value"),
                                IdentifierName("Serialize")),
                            ArgumentList(SeparatedList(new ArgumentSyntax[]
                            {
                            Argument(IdentifierName("_data")),
                            Argument(IdentifierName("offset"))
                            }))))));

                    List<StatementSyntax> supportClassDeserializationBody = new();

                    supportClassDeserializationBody.Add(LocalDeclarationStatement(VariableDeclaration(PredefinedType(Token(SyntaxKind.IntKeyword)),
                            SingletonSeparatedList(VariableDeclarator(Identifier("offset"), null,
                            EqualsValueClause(IdentifierName("initialOffset")))))));

                    supportClassDeserializationBody.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(enumIdl.Name.ToPascalCase() + "Value"),
                        ObjectCreationExpression(IdentifierName(enumIdl.Name.ToPascalCase() + "Type"), ArgumentList(), null))));

                    //supportClassDeserializationBody.Add(ExpressionStatement(AssignmentExpression(
                    //    SyntaxKind.SimpleAssignmentExpression,
                    //    MemberAccessExpression(
                    //        SyntaxKind.SimpleMemberAccessExpression,
                    //        IdentifierName("result"),
                    //        IdentifierName("Type")),
                    //    CastExpression(IdentifierName(enumIdl.Name.ToPascalCase() + "Type"), invocation))));

                    supportClassDeserializationBody.Add(
                        ExpressionStatement(AssignmentExpression(
                            SyntaxKind.AddAssignmentExpression,
                            IdentifierName("offset"),
                            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))));


                    supportClasses.Add(ClassDeclaration(List<AttributeListSyntax>(), ClientGeneratorDefaultValues.PublicPartialModifiers, Identifier(member.Name.ToPascalCase() + "Type"), null, null, List<TypeParameterConstraintClauseSyntax>(), List(fields)));

                    mainClassProperties.Add(PropertyDeclaration(List<AttributeListSyntax>(), ClientGeneratorDefaultValues.PublicModifier, IdentifierName(member.Name.ToPascalCase() + "Type"), default, Identifier(member.Name.ToPascalCase() + "Value"), ClientGeneratorDefaultValues.PropertyAccessorList));
                }

                if (caseStatements.Count > 0)
                {
                    caseStatements.Add(BreakStatement());
                    serializationCases.Add(SwitchSection(SingletonList<SwitchLabelSyntax>(CaseSwitchLabel(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(enumIdl.Name.ToPascalCase() + "Type"), IdentifierName(member.Name)))),
                        List(caseStatements)));

                    caseStatementsDeser.Add(BreakStatement());
                    deSerializationCases.Add(SwitchSection(SingletonList<SwitchLabelSyntax>(CaseSwitchLabel(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(enumIdl.Name.ToPascalCase() + "Type"), IdentifierName(member.Name)))),
                        SingletonList<StatementSyntax>(Block(caseStatementsDeser))));

                }
            }

            if (mainClassProperties.Count == 0)
            {
                return SingletonList<MemberDeclarationSyntax>(EnumDeclaration(
                    List<AttributeListSyntax>(),
                    ClientGeneratorDefaultValues.PublicModifier,
                    Identifier(enumIdl.Name.ToPascalCase()),
                    BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(PredefinedType(Token(SyntaxKind.ByteKeyword))))),
                    SeparatedList(enumMembers)));
            }

            // need to create specific serialization

            var ser = Block(

                    LocalDeclarationStatement(VariableDeclaration(PredefinedType(Token(SyntaxKind.IntKeyword)),
                            SingletonSeparatedList(VariableDeclarator(Identifier("offset"), null,
                            EqualsValueClause(IdentifierName("initialOffset")))))),

                ExpressionStatement(InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("_data"),
                        IdentifierName("WriteU8")),
                    ArgumentList(SeparatedList(new ArgumentSyntax[]
                    {
                        Argument(CastExpression(PredefinedType(Token(SyntaxKind.ByteKeyword)), IdentifierName("Type"))),
                        Argument(IdentifierName("offset"))
                    })))),

                ExpressionStatement(AssignmentExpression(
                    SyntaxKind.AddAssignmentExpression,
                    IdentifierName("offset"),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))),

                SwitchStatement(IdentifierName("Type"), List(serializationCases)),

                ReturnStatement(BinaryExpression(SyntaxKind.SubtractExpression, IdentifierName("offset"), IdentifierName("initialOffset"))));

            mainClassProperties.Add(MethodDeclaration(List<AttributeListSyntax>(),
                        ClientGeneratorDefaultValues.PublicModifier,
                        PredefinedType(Token(SyntaxKind.IntKeyword)),
                        null,
                        Identifier("Serialize"),
                        null,
                        ParameterList(SeparatedList(new ParameterSyntax[] {
                            Parameter(List<AttributeListSyntax>(), TokenList(), ArrayType(PredefinedType(Token(SyntaxKind.ByteKeyword)), SingletonList(ArrayRankSpecifier())), Identifier("_data"), null),
                            Parameter(List<AttributeListSyntax>(), TokenList(), PredefinedType(Token(SyntaxKind.IntKeyword)), Identifier("initialOffset"), null),
                        })),
                        List<TypeParameterConstraintClauseSyntax>(),
                        ser,
                        null));

            mainClassProperties.Add(PropertyDeclaration(List<AttributeListSyntax>(),
                ClientGeneratorDefaultValues.PublicModifier,
                IdentifierName(enumIdl.Name.ToPascalCase() + "Type"),
                default,
                Identifier("Type"),
                ClientGeneratorDefaultValues.PropertyAccessorList));



            List<StatementSyntax> finalizedDesserialization = new();

            finalizedDesserialization.Add(LocalDeclarationStatement(VariableDeclaration(PredefinedType(Token(SyntaxKind.IntKeyword)),
                    SingletonSeparatedList(VariableDeclarator(Identifier("offset"), null,
                    EqualsValueClause(IdentifierName("initialOffset")))))));


            var resultVariableToken = Identifier("result");
            var resulVariable = IdentifierName(resultVariableToken);
            var constructorCallExpression = ObjectCreationExpression(IdentifierName(enumIdl.Name.ToPascalCase()), ArgumentList(), null);

            finalizedDesserialization.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, resulVariable, constructorCallExpression)));

            var enumValueToken = Identifier("resulVal");
            var enumValueVariable = IdentifierName(enumValueToken);

            var invocation = InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("_data"), IdentifierName("GetU8")),
                ArgumentList(SeparatedList(new ArgumentSyntax[]
                {
                    Argument(IdentifierName("offset"))
                })));

            finalizedDesserialization.Add(ExpressionStatement(AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("result"), IdentifierName("Type")),
                CastExpression(IdentifierName(enumIdl.Name.ToPascalCase() + "Type"), invocation))));

            finalizedDesserialization.Add(
                ExpressionStatement(AssignmentExpression(
                    SyntaxKind.AddAssignmentExpression,
                    IdentifierName("offset"),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))));

            //finalizedDesserialization.AddRange(desserializationBody);

            finalizedDesserialization.Add(
                SwitchStatement(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("result"), IdentifierName("Type")), List(deSerializationCases)));

            //foreach (var field in structIdl.Fields)
            //{
            //    desserializationBody.AddRange(GenerateDeserializationSyntaxList(idl.Types, field.Type, MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, resulVariable, IdentifierName(field.Name.ToPascalCase()))));
            //}

            // if account -> public static AccName Deserialize(byte[] data)
            // else ->       public static int Deserialize(byte[] data, int initialOffset, out ObjName result)

            finalizedDesserialization.Add(ReturnStatement(BinaryExpression(SyntaxKind.SubtractExpression, IdentifierName("offset"), IdentifierName("initialOffset"))));
            //IdentifierName(structIdl.Name.ToPascalCase()),
            mainClassProperties.Add(MethodDeclaration(List<AttributeListSyntax>(),
                    ClientGeneratorDefaultValues.PublicStaticModifiers,
                    PredefinedType(Token(SyntaxKind.IntKeyword)),
                    null,
                    Identifier("Deserialize"),
                    null,
                    ParameterList(SeparatedList(new ParameterSyntax[] {
                        Parameter(List<AttributeListSyntax>(), TokenList(), GenericName(Identifier("ReadOnlySpan"), TypeArgumentList(SingletonSeparatedList<TypeSyntax>(PredefinedType(Token(SyntaxKind.ByteKeyword))))), Identifier("_data"), null),
                        Parameter(List<AttributeListSyntax>(), TokenList(), PredefinedType(Token(SyntaxKind.IntKeyword)), Identifier("initialOffset"), null),
                        Parameter(List<AttributeListSyntax>(), TokenList(Token(SyntaxKind.OutKeyword)), IdentifierName(enumIdl.Name.ToPascalCase()), resultVariableToken, null)
                    })),
                    List<TypeParameterConstraintClauseSyntax>(),
                    Block(finalizedDesserialization),
                    null));

            supportClasses.Add(ClassDeclaration(List<AttributeListSyntax>(),
                ClientGeneratorDefaultValues.PublicPartialModifiers,
                Identifier(enumIdl.Name.ToPascalCase()),
                null,
                null,
                List<TypeParameterConstraintClauseSyntax>(),
                List(mainClassProperties)));

            return SingletonList<MemberDeclarationSyntax>(EnumDeclaration(
                List<AttributeListSyntax>(),
                ClientGeneratorDefaultValues.PublicModifier,
                Identifier(enumIdl.Name.ToPascalCase() + "Type"),
                BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(PredefinedType(Token(SyntaxKind.ByteKeyword))))),
                SeparatedList(enumMembers))).AddRange(supportClasses);
        }

        private MemberDeclarationSyntax GenerateEventsSyntaxTree(Idl idl)
        {
            SyntaxList<MemberDeclarationSyntax> events = List<MemberDeclarationSyntax>();

            for (int i = 0; i < idl.Events.Length; i++)
            {


            }

            return NamespaceDeclaration(IdentifierName("Events"), List<ExternAliasDirectiveSyntax>(), List<UsingDirectiveSyntax>(), events);
        }

        private MemberDeclarationSyntax GenerateErrorsSyntaxTree(Idl idl)
        {
            SyntaxNodeOrTokenList errors = new SyntaxNodeOrTokenList();

            for (int i = 0; i < idl.Errors?.Length; i++)
            {
                var dec = EnumMemberDeclaration(Identifier(idl.Errors[i].Name.ToPascalCase()))
                    .WithEqualsValue(
                        EqualsValueClause(
                            LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                Literal(idl.Errors[i].Code))));
                errors = errors.Add(dec);

                if (i < idl.Errors.Length - 1) errors = errors.Add(Token(SyntaxKind.CommaToken));
            }

            var errorsEnum = EnumDeclaration(idl.NamePascalCase + "ErrorKind")
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(
                    SimpleBaseType(PredefinedType(Token(SyntaxKind.UIntKeyword))))))
                .WithMembers(SeparatedList<EnumMemberDeclarationSyntax>(errors));

            return NamespaceDeclaration(IdentifierName("Errors"))
                .WithMembers(SingletonList<MemberDeclarationSyntax>(errorsEnum));
        }

        private MemberDeclarationSyntax GenerateAccountsSyntaxTree(Idl idl)
        {
            List<MemberDeclarationSyntax> accounts = new();

            for (int i = 0; i < idl.Accounts.Length; i++)
            {
                accounts.AddRange(GenerateTypeDeclaration(idl, idl.Accounts[i], false, true));
            }

            return NamespaceDeclaration(IdentifierName("Accounts"), List<ExternAliasDirectiveSyntax>(), List<UsingDirectiveSyntax>(), List(accounts));
        }


        private string GetNormalizedName(string name)
        {
            if (LANG_KEYWORDS.Contains(name))
                return $"@{name}";
            return name;
        }
    }
}