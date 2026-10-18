using System.Collections.Generic;
using UnityEngine;

namespace LegionRush
{
    // Ponte entre a lógica do chefão (BossController: vida, fases, dano) e o modelo dele (animação, VFX, materiais).
    // O controller nunca depende do modelo: sem apresentação, cai nas inclinações simples do placeholder.
    // Um modelo novo (procedural ou FBX com Animator) só precisa de um componente que herde desta classe no visual.
    public abstract class BossPresentation : MonoBehaviour
    {
        public BossController Boss { get; private set; }

        public void Bind(BossController boss)
        {
            Boss = boss;
            OnBound();
        }

        protected virtual void OnBound() { }

        // Chamado todo quadro pelo controller (inclusive dormente). moveRatio: 0 parado, 1 andando a toda.
        public virtual void Tick(float deltaTime, float moveRatio) { }

        public virtual void OnStateChanged(BossController.BossState previous, BossController.BossState state) { }

        // Início do aviso: a animação deve chegar ao impacto em exatamente windupSeconds.
        // points: alvos do ataque (Rain: cada rochedo; demais: um ponto). direction: eixo de ataques em linha.
        public virtual void OnAttackWindup(BossAttack attack, float windupSeconds, float followSeconds,
            IReadOnlyList<Vector3> points, Vector3 direction) { }

        // Impacto principal (fim do aviso): o dano acabou de ser aplicado neste ponto.
        public virtual void OnAttackImpact(BossAttack attack, Vector3 point) { }

        // Avanço de ataques que percorrem o chão (onda de rochas): um passo por trecho percorrido.
        public virtual void OnAttackStep(BossAttack attack, Vector3 point, float halfWidth) { }

        public virtual void OnHit(DamageInfo info) { }
        public virtual void OnPhaseChanged(int phase) { }
        public virtual void OnDeath() { }
    }
}
