import { MirrorSharpEditor } from "./mirrorsharp-editor";
import { MonacoEditor } from "./monaco-editor";
import { CodeEditorProps } from "./types";

const EditorType = 'monaco';

export function CodeEditor(props: CodeEditorProps) {
  return (
    <div className="flex-1 overflow-y-auto h-full" style={{height:"calc(100dvh - 50px)"}}>
      {
        EditorType === 'monaco' ?
          <MonacoEditor {...props} />
          :
          <MirrorSharpEditor {...props} />
      }
    </div>
  )
}