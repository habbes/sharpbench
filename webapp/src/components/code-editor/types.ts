export interface CodeEditorProps {
    serverUrl: string;
    onTextChange?: (text: string) => void;
    code?: string;
}