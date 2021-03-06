﻿using System;

namespace Trophy.Analysis.Types {
    public enum TypeCopiability {
        Unconditional, Conditional
    }

    public interface ITrophyType : IEquatable<ITrophyType> {
        public static ITrophyType Boolean { get; } = new BoolType();

        public static ITrophyType Integer { get; } = new IntType();

        public static ITrophyType Void { get; } = new VoidType();

        public TypeCopiability GetCopiability(ITypesRecorder types);

        public bool HasDefaultValue(ITypesRecorder types);

        public bool IsBoolType => false;

        public bool IsIntType => false;

        public bool IsVoidType => false;

        public IOption<ArrayType> AsArrayType() => Option.None<ArrayType>();

        public IOption<FixedArrayType> AsFixedArrayType() => Option.None<FixedArrayType>();

        public IOption<VarRefType> AsVariableType() => Option.None<VarRefType>();

        public IOption<SingularFunctionType> AsSingularFunctionType() => Option.None<SingularFunctionType>();

        public IOption<IdentifierPath> AsNamedType() => Option.None<IdentifierPath>();

        public IOption<FunctionType> AsFunctionType() => Option.None<FunctionType>();

        public IOption<GenericType> AsGenericType() => Option.None<GenericType>();

        public IOption<MetaType> AsMetaType() => Option.None<MetaType>();
    }
}