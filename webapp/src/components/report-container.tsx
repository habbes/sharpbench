import { JobCompleteMessage } from '@/types';
import Markdown from "react-markdown";
import remarkGfm from 'remark-gfm'

export function ReportContainer({ message } : { message? : JobCompleteMessage }) {
  return (
    <>
    {
      message ?
        message.Job.ExitCode === 0 ?
          <Markdown remarkPlugins={[remarkGfm]}>{ message.Job.MarkdownResult }</Markdown> :
          <div className="font-mono">
            Execution failed with exit code { message.Job.ExitCode }. Check out the logs for more details.
          </div>
        :
        <div>Results not ready...</div>
    }
    </>
  );
}
