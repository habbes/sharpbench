import { MirrorSharpEditor } from "./mirrorsharp-editor";
import { MonacoEditor } from "./monaco-editor";
import { CodeEditorProps } from "./types";

// I switched from MirrorSharp to editor to Monaco because
// MirrorSharp was buggy to the point that it was not feasible to write
// a complete benchmark in the editor. The cursor would jump to seemingly
// random location, not sure what the issue is. If I figure out how to fix it,
// I'll go back to Mirrorsharp since it has out-of-the box support for
// C# intellisense and auto-complete
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