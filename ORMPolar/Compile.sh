#!/bin/bash
mcs \
-pkg:dotnet \
/reference:"/media/data/My_Documents/Coding/_VSprojects/students/Common/DLLs/PolarDB.dll" \
/reference:System.ComponentModel.DataAnnotations.dll *.cs \
/doc:"/media/data/My_Documents/Coding/_VSprojects/students/ORMPolar/Documentation/AutoDoc.doc" \
/target:exe
