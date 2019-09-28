# Changelog

## 0.3.1 (28 Sep 2019)
- Make const field as readonly

## 0.3.0 (08 Sep 2019)
- Add support for simple bitfields
- Fix potential issue with null reference exceptions with comment
- Exclude inline functions, C++ methods for DllImport functions
- Fix issue with MarshalAs attributes and force a clone
- Start to add support for virtual methods/interface (not yet working)
- Improve codegen for enum 
- Add support for base type for structs

## 0.2.4 (18 Jun 2019)
- Add CSharpMarshalAttribute.MarshalTypeRef  

## 0.2.3 (18 Jun 2019)
- Bump CppAst version to 0.5.7

## 0.2.2 (16 Jun 2019)
- Add support for explicit cast for const/enum items
- Fix const fields
- Add new mapping rules: InitValue, MarshalAs

## 0.2.1 (16 Jun 2019)
- Fix issue with type remapping

## 0.2.0 (16 Jun 2019)
- Add support for Discard()
- Fix comment for parameter (do not escape)

## 0.1.0 (15 Jun 2019)
- Initial version
