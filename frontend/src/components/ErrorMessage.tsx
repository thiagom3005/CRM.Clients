interface Props {
  message: string;
  onRetry?: () => void;
}

export default function ErrorMessage({ message, onRetry }: Props) {
  return (
    <div className="error-message" role="alert">
      <span>{message}</span>
      {onRetry && (
        <button type="button" className="btn-retry" onClick={onRetry}>
          Recarregar
        </button>
      )}
    </div>
  );
}
