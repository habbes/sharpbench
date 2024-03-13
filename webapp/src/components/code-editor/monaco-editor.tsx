import { useRef, useEffect } from "react";
import Editor, { OnMount } from "@monaco-editor/react";
import { CodeEditorProps } from "./types";

export function MonacoEditor({ code, onTextChange }: CodeEditorProps) {
  const editorRef = useRef<Parameters<OnMount>[0]|null>(null);

  useEffect(() => {
    if (!editorRef.current) return;
    if (code === undefined) return;
  
    editorRef.current.setValue(code);
  }, [code, onTextChange]);

  const handleEditorMount: OnMount = (editor) => {
    editorRef.current = editor;
  }

  return (
    <Editor
      height="100%"
      defaultLanguage={"csharp"}
      defaultValue={code}
      onMount={handleEditorMount}
      onChange={value => value !== undefined && onTextChange(value)}
    />
  );
}