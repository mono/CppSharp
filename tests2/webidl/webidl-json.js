import * as fs from "fs";
import webidl2 from "webidl2";

const fileName = "/home/joao/dev/CppSharp/tests2/webidl/wpt/interfaces/dom.idl"
const outputFileName = "dom.json"

const content = fs.readFileSync(fileName);
const parsingStructure = webidl2.parse(content.toString());
fs.writeFileSync(outputFileName, JSON.stringify(parsingStructure, null, 2));
