import Editor from "@monaco-editor/react";
import { CodeEditorProps } from "./types";

export function MonacoEditor(props: CodeEditorProps) {
    return (
        <Editor height="100%" defaultLanguage={"csharp"} defaultValue={props.code} />
    );
}