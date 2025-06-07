using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Complete.Interfaces;

namespace Complete.GameStates
{
    /// <summary>
    /// ゲーム状態を管理するマネージャークラス
    /// 単一責任の原則に従って状態管理のみを担当
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        private IGameState _currentState;
        private readonly Queue<IGameState> _stateQueue = new Queue<IGameState>();
        private bool _isTransitioning = false;

        /// <summary>
        /// 現在の状態
        /// </summary>
        public IGameState CurrentState => _currentState;

        /// <summary>
        /// 状態を追加する
        /// </summary>
        /// <param name="state">追加する状態</param>
        public void EnqueueState(IGameState state)
        {
            _stateQueue.Enqueue(state);
        }

        /// <summary>
        /// 次の状態に遷移する
        /// </summary>
        public void TransitionToNextState()
        {
            if (_stateQueue.Count > 0 && !_isTransitioning)
            {
                StartCoroutine(TransitionCoroutine());
            }
        }

        /// <summary>
        /// 指定した状態に直接遷移する
        /// </summary>
        /// <param name="newState">遷移先の状態</param>
        public void TransitionToState(IGameState newState)
        {
            if (!_isTransitioning)
            {
                StartCoroutine(TransitionToStateCoroutine(newState));
            }
        }

        private IEnumerator TransitionCoroutine()
        {
            _isTransitioning = true;

            // 現在の状態を終了
            _currentState?.Exit();

            // 次の状態を開始
            _currentState = _stateQueue.Dequeue();
            yield return StartCoroutine(_currentState.Enter());

            _isTransitioning = false;
        }

        private IEnumerator TransitionToStateCoroutine(IGameState newState)
        {
            _isTransitioning = true;

            // 現在の状態を終了
            _currentState?.Exit();

            // 新しい状態を開始
            _currentState = newState;
            yield return StartCoroutine(_currentState.Enter());

            _isTransitioning = false;
        }
    }
} 