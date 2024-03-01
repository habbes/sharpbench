import { useState } from 'react'
import { PlayIcon } from '@radix-ui/react-icons';
import './App.css'
import { Button } from "@/components/ui/button";
import { CodeEditor } from "@/components/code-editor";
import { ResultsContainer } from "@/components/results-container";
import { DoubleArrowRightIcon, DoubleArrowLeftIcon } from "@radix-ui/react-icons";
import { INITIAL_CODE } from "./initial-code";

// TODO this should be configure using env vars
const API_URL = import.meta.env.VITE_API_BASE_URL || "http://localhost:5176";
const WS_URL = import.meta.env.VITE_WS_BASE_URL || API_URL.replace("http", "ws");
const EDITOR_SERVICE_URL = `${WS_URL}/mirrorsharp`;
const JOB_UPDATES_URL = `${WS_URL}/jobs-ws`;
console.log('jobs updates at', JOB_UPDATES_URL);

export function App() {
  const [code, setCode] = useState(INITIAL_CODE);
  const [isShowingJobsSidebar, setIsShowingJobsSidebar] = useState(false);

  function handleRun() {
    if (!code) return;
    submitCodeRun(code);
  }

  function toggleShowJobsSidebar() {
    setIsShowingJobsSidebar(!isShowingJobsSidebar);
  }

  return (
    <main className="h-screen bg-red flex flex-col">
      <div className="h-[50px] pr-2 flex items-center border-b border-b-gray-200 shadow-sm justify-between">
        <div className="flex items-center h-full">
          {!isShowingJobsSidebar ?
            (
              <div onClick={toggleShowJobsSidebar}
                className="px-4 border-r border-r-gray-200 mr-4 h-full flex items-center cursor-pointer"
                title="Reveal job history"
              >
                <DoubleArrowRightIcon />
              </div>
            ) :
            (
              <div onClick={toggleShowJobsSidebar}
                className="flex w-[300px] h-full px-4 items-center border-r border-r-gray-200 justify-between mr-4"
              >
                <div className="text-sm font-semibold">
                  Jobs History
                </div>
                <div className="cursor-pointer" title="Hide job history">
                  <DoubleArrowLeftIcon />
                </div>
              </div>
            )
          }
          <Button onClick={handleRun} className="inline-flex flex gap-2 items-center"><PlayIcon /> Run</Button>
        </div>
        <div>
          <div className="mr-5">
            <span className="font-semibold">sharpbench</span>
          </div>
        </div>
      </div>
      <div className="flex flex-1" style={{height:"calc(100dvh - 50px)"}}>
        <div className="flex-1 h-full">
          <CodeEditor
            serverUrl={EDITOR_SERVICE_URL}
            initialCode={INITIAL_CODE}
            onTextChange={setCode}
          />
        </div>
        <div className="flex-1 h-full border-l border-l-gray-200">
          <ResultsContainer jobUpdatesUrl={JOB_UPDATES_URL} />
        </div>
      </div>
    </main>
  )
}

async function submitCodeRun(code: string) {
  console.log('submitting code run', code);
  const resp = await fetch(`${API_URL}/run`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      code: code
    }),
    mode: 'cors'
  });

  const data = await resp.json();
  console.log('code run result', data);
  return data;
}

