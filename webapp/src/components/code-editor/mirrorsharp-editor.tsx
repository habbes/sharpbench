import { useRef, useEffect } from 'react'
import "mirrorsharp-codemirror-6-preview/mirrorsharp.css";
import mirrorsharp, { MirrorSharpInstance } from 'mirrorsharp-codemirror-6-preview';
import { CodeEditorProps } from "./types";
import { logger } from '@/logger';

const LANGUAGE = 'C#';

export function MirrorSharpEditor({
    onTextChange,
    serverUrl,
    code
} : CodeEditorProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const ms = useRef<MirrorSharpInstance<void>>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    if (ms.current && code) {
      ms.current.setText(code);
    }
    else if (!ms.current) {
      logger.log('rendering ms');
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
      // @ts-ignore
      ms.current = mirrorsharp(
        containerRef.current, {
          serviceUrl: serverUrl,
          language: LANGUAGE,
          text: code,
          serverOptions: { 'x-mode': 'regular' },
          on: {
              textChange: getText => onTextChange && onTextChange(getText())
          }
        });
    }
  }, [code, onTextChange, serverUrl]);

  return (
    <div style={{height:"100%"}} ref={containerRef}></div>
  );
}
